namespace Jardin.Inscripciones.Contracts;

public interface IModuloUsoDatosSolicitud
{
    // Registro interno para los consumidores reales; no crea la medición/propuesta referenciada.
    Task RegistrarUsoAsync(
        Guid institucionId,
        Guid solicitudId,
        DatosUsoSolicitud uso,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarImpactoSolicitudRespuesta> ConsultarImpactoAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
}
