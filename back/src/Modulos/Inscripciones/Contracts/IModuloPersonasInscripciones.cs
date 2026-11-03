namespace Jardin.Inscripciones.Contracts;

public interface IModuloPersonasInscripciones
{
    Task<RegistrarAspiranteRespuesta> RegistrarAspiranteAsync(
        Guid institucionId,
        RegistrarAspirantePeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarAspiranteRespuesta?> ConsultarAspiranteAsync(
        Guid institucionId,
        Guid aspiranteId,
        CancellationToken tokenCancelacion = default);
    Task<RegistrarResponsableRespuesta> RegistrarResponsableAsync(
        Guid institucionId,
        RegistrarResponsablePeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarResponsableRespuesta?> ConsultarResponsableAsync(
        Guid institucionId,
        Guid responsableId,
        CancellationToken tokenCancelacion = default);
    Task<BuscarResponsablesRespuesta> BuscarResponsablesAsync(
        Guid institucionId,
        BuscarResponsablesPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<VincularResponsableRespuesta> VincularResponsableAsync(
        Guid institucionId,
        Guid aspiranteId,
        VincularResponsablePeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<DatosCambioInscripciones>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
}
