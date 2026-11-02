namespace Jardin.Configuracion.Contracts;

// Operaciones públicas de HU-01; la autorización se aplica también en Application.
public interface IModuloConfiguracion
{
    Task<RegistrarInstitucionRespuesta> RegistrarInstitucionAsync(RegistrarInstitucionPeticion peticion, CancellationToken tokenCancelacion = default);
    Task<ConsultarInstitucionRespuesta?> ConsultarInstitucionAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarInstitucionRespuesta>> ListarInstitucionesAsync(CancellationToken tokenCancelacion = default);
    Task<ActualizarInstitucionRespuesta> ActualizarInstitucionAsync(
        Guid institucionId,
        ActualizarInstitucionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<DatosCambioInstitucion>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<ConsultarImpactoUbicacionRespuesta> ConsultarImpactoUbicacionAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task RegistrarUsoUbicacionAsync(
        Guid institucionId,
        DatosUsoUbicacion uso,
        CancellationToken tokenCancelacion = default);
    Task<bool> ComprobarVigenciaUsoAsync(
        Guid institucionId,
        DatosUsoUbicacion uso,
        CancellationToken tokenCancelacion = default);
}
