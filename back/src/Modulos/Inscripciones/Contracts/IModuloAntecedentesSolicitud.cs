namespace Jardin.Inscripciones.Contracts;

public interface IModuloAntecedentesSolicitud
{
    Task<ConsultarAntecedentesRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task<RegistrarDocumentoRespuesta> RegistrarDocumentoAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarDocumentoPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<RevisarAntecedenteRespuesta> RevisarDocumentoAsync(
        Guid institucionId,
        Guid solicitudId,
        Guid documentoId,
        RevisarDocumentoPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<RegistrarCondicionRespuesta> RegistrarCondicionAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarCondicionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<RevisarAntecedenteRespuesta> RevisarCondicionAsync(
        Guid institucionId,
        Guid solicitudId,
        Guid condicionId,
        RevisarCondicionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<RevisarAntecedenteRespuesta> CorregirCondicionAsync(
        Guid institucionId,
        Guid solicitudId,
        Guid condicionId,
        CorregirCondicionPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
