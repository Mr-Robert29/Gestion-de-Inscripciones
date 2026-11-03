namespace Jardin.Configuracion.Contracts;

// Operaciones de períodos del mismo módulo Configuración.
public interface IModuloPeriodos
{
    Task<CrearPeriodoRespuesta> CrearPeriodoAsync(
        Guid institucionId,
        CrearPeriodoPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarPeriodoRespuesta?> ConsultarPeriodoAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarPeriodoRespuesta>> ListarPeriodosAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<CambiarRecepcionRespuesta> AbrirRecepcionAsync(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<CambiarRecepcionRespuesta> CerrarRecepcionAsync(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    // Ejecuta la operación mientras se conserva el estado del período frente a modificaciones concurrentes.
    Task<T> EjecutarConPeriodoProtegidoAsync<T>(
        Guid periodoId,
        Func<ConsultarPeriodoRespuesta, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}
