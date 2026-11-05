namespace Jardin.Vacantes.Contracts;

public interface IModuloCasosRevision
{
    Task<IReadOnlyList<DatosCasoRevision>> ListarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosCasoRevision>> ListarParaReportesAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);

    Task<DatosCasoRevision?> ConsultarAsync(
        Guid casoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatosHistorialCasoRevision>> ConsultarHistorialAsync(
        Guid casoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosHistorialCasoRevision>> ConsultarHistorialParaReportesAsync(
        Guid casoId,
        CancellationToken cancellationToken = default);

    Task<DatosCasoRevision> ResolverAsync(
        Guid casoId,
        ResolverCasoRevisionPeticion peticion,
        CancellationToken cancellationToken = default);

    Task<DatosCasoRevision> ReabrirAsync(
        Guid casoId,
        ReabrirCasoRevisionPeticion peticion,
        CancellationToken cancellationToken = default);
}
