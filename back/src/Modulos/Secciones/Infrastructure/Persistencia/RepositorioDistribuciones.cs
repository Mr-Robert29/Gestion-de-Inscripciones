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
            .Include(x => x.Asignaciones)
            .FirstOrDefaultAsync(x => x.Id == propuestaId, cancellationToken);

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
                _contexto.PropuestasDistribucion.Update(propuesta);
                await _contexto.AsignacionesSeccion.AddRangeAsync(asignaciones, cancellationToken);
                await _contexto.SaveChangesAsync(cancellationToken);
                return true;
            },
            cancellationToken);
    }
}
