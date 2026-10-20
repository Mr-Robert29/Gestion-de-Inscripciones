namespace Jardin.Secciones.Contracts;

public interface IModuloSecciones
{
    Task<Guid> DefinirSeccionAsync(DefinirSeccionPeticion peticion, CancellationToken cancellationToken = default);
    Task<DatosSeccion?> ObtenerSeccionPorIdAsync(Guid seccionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosSeccion>> ListarSeccionesPorPeriodoAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task<bool> CambiarComposicionSeccionAsync(Guid seccionId, CambiarComposicionSeccionPeticion peticion, CancellationToken cancellationToken = default);
    Task<bool> CambiarCapacidadSeccionAsync(Guid seccionId, CambiarCapacidadSeccionPeticion peticion, CancellationToken cancellationToken = default);
}
