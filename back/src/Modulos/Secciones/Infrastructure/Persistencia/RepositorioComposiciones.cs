using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Domain;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class RepositorioComposiciones(ContextoSecciones contexto) : IRepositorioComposiciones
{
    // Implementa IRepositorioComposiciones.ConSeccionProtegidaAsync: lee y cambia bajo el bloqueo compartido con HU-21.
    public async Task<T> ConSeccionProtegidaAsync<T>(
        Guid periodoId,
        Guid seccionId,
        Func<Seccion, IReadOnlyList<Guid>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion)
    {
        var referencia = await contexto.Secciones.AsNoTracking().SingleOrDefaultAsync(x => x.Id == seccionId && x.PeriodoId == periodoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la sección en el período.");
        return await ProteccionSecciones.EjecutarAsync(
            contexto,
            ProteccionSecciones.Oferta(periodoId, referencia.AulaId, referencia.TurnoId),
            async () =>
            {
                if (await contexto.Secciones.CountAsync(x => x.PeriodoId == periodoId && x.AulaId == referencia.AulaId && x.TurnoId == referencia.TurnoId && x.Activa, tokenCancelacion) != 1)
                    throw new InvalidOperationException("La misma aula y turno tienen varias secciones; primero definí la partición de su capacidad.");
                var seccion = await contexto.Secciones.SingleAsync(x => x.Id == seccionId, tokenCancelacion);
                await contexto.Entry(seccion).ReloadAsync(tokenCancelacion);
                var asignados = await contexto.AsignacionesSeccion.Where(x => x.SeccionId == seccionId && x.Estado == EstadoAsignacionSeccion.Activa)
                    .Select(x => x.IngresoId).ToArrayAsync(tokenCancelacion);
                return await operacion(seccion, asignados, tokenCancelacion);
            },
            tokenCancelacion);
    }

    // Implementa IRepositorioComposiciones.GuardarAsync: sección, historial y revisión de borradores se guardan juntos.
    public async Task<IReadOnlyList<Guid>> GuardarAsync(CambioComposicionSeccion cambio, CancellationToken tokenCancelacion)
    {
        contexto.CambiosComposicion.Add(cambio);
        var propuestas = await contexto.PropuestasDistribucion.Where(x => x.PeriodoId == cambio.PeriodoId && x.Estado == EstadoPropuestaDistribucion.Borrador)
            .ToArrayAsync(tokenCancelacion);
        foreach (var propuesta in propuestas) propuesta.MarcarDesactualizada();
        await contexto.SaveChangesAsync(tokenCancelacion);
        return propuestas.Select(x => x.Id).ToArray();
    }

    // Implementa IRepositorioComposiciones.ConsultarHistorialAsync conservando todos los cambios anteriores.
    public async Task<IReadOnlyList<CambioComposicionSeccion>> ConsultarHistorialAsync(Guid seccionId, CancellationToken tokenCancelacion)
        => (await contexto.CambiosComposicion.AsNoTracking().Where(x => x.SeccionId == seccionId).ToArrayAsync(tokenCancelacion))
            .OrderBy(x => x.Fecha).ToArray();
}
