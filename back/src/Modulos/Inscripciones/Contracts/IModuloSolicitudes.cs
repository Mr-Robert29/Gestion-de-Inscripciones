namespace Jardin.Inscripciones.Contracts;

public interface IModuloSolicitudes
{
    Task<RegistrarSolicitudRespuesta> RegistrarAsync(
        Guid institucionId,
        Guid periodoId,
        RegistrarSolicitudPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarSolicitudRespuesta?> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task<ListarSolicitudesRespuesta> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        ListarSolicitudesPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
