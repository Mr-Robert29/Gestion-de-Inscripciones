using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioCorreccionesSolicitudes
{
    Task<SolicitudInscripcion?> ObtenerEditableAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<DatosSolicitudEncontrada>> BuscarAsync(
        Guid institucionId,
        BuscarSolicitudesPeticion filtro,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default);
    Task GuardarAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
}
