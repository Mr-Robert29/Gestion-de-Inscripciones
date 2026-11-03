using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioAntecedentesSolicitud
{
    Task<SolicitudInscripcion?> ObtenerAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task AgregarDocumentoAsync(
        SolicitudInscripcion solicitud,
        DocumentacionSolicitud documento,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task AgregarCondicionAsync(
        SolicitudInscripcion solicitud,
        CondicionPrioridad condicion,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task GuardarRevisionAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
}
