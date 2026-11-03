using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Persistencia;

// Período e historial institucional se guardan en la misma transacción.
public interface IRepositorioPeriodos
{
    Task<PeriodoInscripcion?> ObtenerAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<PeriodoInscripcion>> ListarAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task AgregarAsync(
        PeriodoInscripcion periodo,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    Task ActualizarAsync(
        PeriodoInscripcion periodo,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    // Ejecuta la operación mientras se conserva el estado del período frente a modificaciones concurrentes.
    Task<T> EjecutarConBloqueoAsync<T>(
        Guid periodoId,
        Func<PeriodoInscripcion, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}
