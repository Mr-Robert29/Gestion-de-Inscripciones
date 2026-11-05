namespace Jardin.Secciones.Contracts;

public interface IModuloSecciones
{
    Task<Guid> DefinirSeccionAsync(DefinirSeccionPeticion peticion, CancellationToken cancellationToken = default);
    Task<DatosSeccion?> ObtenerSeccionPorIdAsync(Guid seccionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosSeccion>> ListarSeccionesPorPeriodoAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosPropuestaDistribucion>> ListarPropuestasDistribucionAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task<bool> CambiarComposicionSeccionAsync(Guid seccionId, CambiarComposicionSeccionPeticion peticion, CancellationToken cancellationToken = default);
    Task<DatosPropuestaDistribucion> GenerarDistribucionAsync(Guid periodoId, Guid usuarioId, CancellationToken cancellationToken = default);
    Task<DatosPropuestaDistribucion?> ObtenerPropuestaDistribucionAsync(Guid propuestaId, CancellationToken cancellationToken = default);
    Task<DatosPropuestaDistribucion> ModificarAsignacionAsync(Guid propuestaId, ModificarAsignacionPropuestaPeticion peticion, Guid usuarioId, CancellationToken cancellationToken = default);
    Task<DatosPropuestaDistribucion?> ConfirmarDistribucionAsync(Guid propuestaId, Guid usuarioId, CancellationToken cancellationToken = default);
}
