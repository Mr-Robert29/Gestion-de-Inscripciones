namespace Jardin.Inscripciones.Contracts;

public interface IModuloRevisionDomicilios
{
    Task<ConsultarRevisionDomicilioRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarRevisionDomicilioRespuesta> RegistrarAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarRevisionDomicilioPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
