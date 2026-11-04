namespace Jardin.Secciones.Contracts;

// Integración interna: el módulo llamador debe autorizar el período y la operación.
public interface IModuloSeccionesEspera
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
        Func<IReadOnlyList<DatosSeccion>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}
