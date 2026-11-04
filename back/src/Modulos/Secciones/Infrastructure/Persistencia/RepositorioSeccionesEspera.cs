using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Domain;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class RepositorioSeccionesEspera : IRepositorioSeccionesEspera
{
    private readonly ContextoSecciones _contexto;
    public RepositorioSeccionesEspera(ContextoSecciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioSeccionesEspera.DarBajaAsync: registra el retiro y actualiza sus asignaciones juntos.
    public async Task DarBajaAsync(IngresoRetiradoSecciones retiro, CancellationToken tokenCancelacion = default)
    {
        await ProteccionSecciones.EjecutarAsync(
            _contexto,
            $"ingreso:{retiro.IngresoId:N}",
            async () =>
            {
                var existente = await _contexto.IngresosRetirados.SingleOrDefaultAsync(x => x.IngresoId == retiro.IngresoId, tokenCancelacion);
                if (existente is not null)
                {
                    if (existente.PeriodoId != retiro.PeriodoId) throw new InvalidOperationException("El retiro no corresponde al período.");
                    return true;
                }
                var asignaciones = await _contexto.AsignacionesSeccion.Where(x => x.IngresoId == retiro.IngresoId && x.Estado == EstadoAsignacionSeccion.Activa)
                    .ToArrayAsync(tokenCancelacion);
                foreach (var asignacion in asignaciones)
                {
                    var seccion = await _contexto.Secciones.SingleAsync(x => x.Id == asignacion.SeccionId, tokenCancelacion);
                    if (seccion.PeriodoId != retiro.PeriodoId) throw new InvalidOperationException("Una asignación no corresponde al período del ingreso.");
                    _contexto.CambiosBajaAsignaciones.Add(new CambioBajaAsignacion(asignacion, retiro));
                    asignacion.DarDeBaja(retiro.UsuarioId, retiro.Fecha, retiro.Motivo);
                }
                _contexto.IngresosRetirados.Add(retiro);
                await _contexto.SaveChangesAsync(tokenCancelacion);
                return true;
            },
            tokenCancelacion);
    }

    // Implementa IRepositorioSeccionesEspera.ConOfertaProtegidaAsync: coordina lectores operativos y cambios de composición.
    public Task<T> ConOfertaProtegidaAsync<T>(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        Func<IReadOnlyList<Seccion>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => ProteccionSecciones.EjecutarAsync(
            _contexto,
            ProteccionSecciones.Oferta(periodoId, aulaId, turnoId),
            async () => await operacion(await _contexto.Secciones.AsNoTracking()
                .Where(x => x.PeriodoId == periodoId && x.AulaId == aulaId && x.TurnoId == turnoId && x.Activa).ToArrayAsync(tokenCancelacion), tokenCancelacion),
            tokenCancelacion);
}
