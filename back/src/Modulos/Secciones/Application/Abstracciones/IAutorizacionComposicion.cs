namespace Jardin.Secciones.Application.Abstracciones;

public interface IAutorizacionComposicion
{
    Task<Guid> AutorizarDistribucionAsync(
        Guid periodoId,
        bool confirmar,
        CancellationToken tokenCancelacion = default);
    Task<Guid> AutorizarCambioAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<Guid> AutorizarConsultaHistorialAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
