using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class RepositorioDistribuciones : IRepositorioDistribuciones
{
    private readonly ContextoSecciones _contexto;

    public RepositorioDistribuciones(ContextoSecciones contexto)
    {
        _contexto = contexto;
    }

    // Implementa IRepositorioDistribuciones.ContarAsignacionesActivasAsync para descontar las plazas ya distribuidas.
    public async Task<IReadOnlyDictionary<Guid, int>> ContarAsignacionesActivasAsync(Guid periodoId, CancellationToken cancellationToken = default)
        => await _contexto.AsignacionesSeccion
            .Where(x => x.Estado == Domain.EstadoAsignacionSeccion.Activa &&
                _contexto.Secciones.Any(s => s.Id == x.SeccionId && s.PeriodoId == periodoId))
            .GroupBy(x => x.SeccionId)
            .Select(x => new { SeccionId = x.Key, Cantidad = x.Count() })
            .ToDictionaryAsync(x => x.SeccionId, x => x.Cantidad, cancellationToken);

    public Task<int> ObtenerUltimaVersionAsync(Guid periodoId, CancellationToken cancellationToken = default) =>
        _contexto.PropuestasDistribucion
            .Where(x => x.PeriodoId == periodoId)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken)
            .ContinueWith(x => x.Result ?? 0, cancellationToken);

    public async Task AgregarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default)
    {
        await _contexto.PropuestasDistribucion.AddAsync(propuesta, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public Task<PropuestaDistribucion?> ObtenerPorIdAsync(Guid propuestaId, CancellationToken cancellationToken = default) =>
        _contexto.PropuestasDistribucion
            .Include("_asignaciones")
            .FirstOrDefaultAsync(x => x.Id == propuestaId, cancellationToken);

    public async Task<IReadOnlyList<PropuestaDistribucion>> ListarPorPeriodoAsync(Guid periodoId, CancellationToken cancellationToken = default)
        => await _contexto.PropuestasDistribucion
            .Include("_asignaciones")
            .Where(x => x.PeriodoId == periodoId)
            .OrderByDescending(x => x.Version)
            .ToArrayAsync(cancellationToken);

    public async Task ActualizarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default)
    {
        _contexto.PropuestasDistribucion.Update(propuesta);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteAsignacionActivaAsync(Guid ingresoId, CancellationToken cancellationToken = default) =>
        _contexto.AsignacionesSeccion.AnyAsync(
            x => x.IngresoId == ingresoId && x.Estado == Domain.EstadoAsignacionSeccion.Activa,
            cancellationToken);

    // Implementa IRepositorioDistribuciones.ConfirmarAsync sin reactivar ingresos retirados.
    public async Task ConfirmarAsync(
        PropuestaDistribucion propuesta,
        IReadOnlyList<AsignacionSeccion> asignaciones,
        CancellationToken cancellationToken = default)
    {
        await ProteccionSecciones.EjecutarAsync(
            _contexto,
            $"distribucion:{propuesta.PeriodoId:N}",
            async () =>
            {
                foreach (var ingresoId in asignaciones.Select(x => x.IngresoId).Distinct().Order())
                {
                    await ProteccionSecciones.BloquearAsync(_contexto, $"ingreso:{ingresoId:N}", cancellationToken);
                    if (await _contexto.IngresosRetirados.AnyAsync(x => x.IngresoId == ingresoId, cancellationToken))
                        throw new InvalidOperationException("El ingreso fue retirado y no admite una asignación activa.");
                }
                var secciones = new List<Seccion>();
                foreach (var seccionId in asignaciones.Select(x => x.SeccionId).Distinct().Order())
                {
                    var referencia = await _contexto.Secciones.AsNoTracking().SingleAsync(x => x.Id == seccionId, cancellationToken);
                    await ProteccionSecciones.BloquearAsync(
                        _contexto,
                        ProteccionSecciones.Oferta(referencia.PeriodoId, referencia.AulaId, referencia.TurnoId),
                        cancellationToken);
                    secciones.Add(await _contexto.Secciones.AsNoTracking().SingleAsync(x => x.Id == seccionId, cancellationToken));
                }
                var guardada = await _contexto.PropuestasDistribucion.AsNoTracking().SingleOrDefaultAsync(x => x.Id == propuesta.Id, cancellationToken);
                if (guardada is not null && guardada.Estado != Domain.EstadoPropuestaDistribucion.Borrador)
                    throw new InvalidOperationException("La propuesta dejó de estar vigente; revisá la composición actual.");
                var edades = await _contexto.AsignacionesPropuesta.AsNoTracking()
                    .Where(x => x.PropuestaDistribucionId == propuesta.Id).ToArrayAsync(cancellationToken);
                foreach (var seccion in secciones)
                {
                    var nuevas = asignaciones.Where(x => x.SeccionId == seccion.Id).ToArray();
                    if (!seccion.Activa || nuevas.Any(x => !edades.Any(e => e.IngresoId == x.IngresoId && seccion.EsCompatibleCon(e.Edad, e.TurnoId))))
                        throw new InvalidOperationException("La composición cambió o hay una asignación incompatible.");
                    var existentes = await _contexto.AsignacionesSeccion.CountAsync(x => x.SeccionId == seccion.Id && x.Estado == Domain.EstadoAsignacionSeccion.Activa, cancellationToken);
                    if (existentes + nuevas.Length > seccion.Capacidad)
                        throw new InvalidOperationException("La sección no dispone de capacidad para confirmar.");
                }
                _contexto.PropuestasDistribucion.Update(propuesta);
                await _contexto.AsignacionesSeccion.AddRangeAsync(asignaciones, cancellationToken);
                await _contexto.SaveChangesAsync(cancellationToken);
                return true;
            },
            cancellationToken);
    }
}
