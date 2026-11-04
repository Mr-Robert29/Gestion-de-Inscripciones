using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Vacantes.Application.Propuestas;

public interface IIntegracionSeccionesEspera
{
    Task DarBajaAsignacionesAsync(
        Guid periodoId,
        Guid ingresoId,
        Guid usuarioId,
        string motivo,
        CancellationToken tokenCancelacion = default);
    Task<T> ConOfertaProtegidaAsync<T>(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        Func<IReadOnlyList<ComposicionVacante>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}
