namespace Jardin.Inscripciones.Contracts;

public interface IModuloDuplicadosSolicitud
{
    Task<DetectarDuplicadosRespuesta> DetectarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default);
    Task<ListarDuplicadosRespuesta> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        ListarDuplicadosPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarDuplicadoRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid alertaId,
        CancellationToken tokenCancelacion = default);
    Task<ResolverDuplicadoRespuesta> ResolverAsync(
        Guid institucionId,
        Guid alertaId,
        ResolverDuplicadoPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
