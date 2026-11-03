using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioSolicitudes
{
    Task AgregarAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task<SolicitudInscripcion?> ObtenerAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<SolicitudInscripcion>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default);
}
