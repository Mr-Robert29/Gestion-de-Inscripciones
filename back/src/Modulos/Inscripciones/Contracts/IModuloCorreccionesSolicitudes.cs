namespace Jardin.Inscripciones.Contracts;

public interface IModuloCorreccionesSolicitudes
{
    Task<BuscarSolicitudesRespuesta> BuscarAsync(
        Guid institucionId,
        BuscarSolicitudesPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<CorregirSolicitudRespuesta> CorregirAsync(
        Guid institucionId,
        Guid solicitudId,
        CorregirSolicitudPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
