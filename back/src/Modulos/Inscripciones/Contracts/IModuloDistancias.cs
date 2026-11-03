namespace Jardin.Inscripciones.Contracts;

public interface IModuloDistancias
{
    Task<ConsultarDistanciaRespuesta> RegistrarManualAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarDistanciaManualPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarDistanciaRespuesta>> ConsultarHistorialAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
}
