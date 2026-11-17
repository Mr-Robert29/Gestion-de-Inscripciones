namespace Jardin.Vacantes.Contracts;

public interface IModuloPrioridades
{
    Task<DatosEstadoProcesamientoPrioridades> ConsultarEstadoAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosPrioridad>> ProcesarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosPrioridad>> EvaluarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosEvaluacionPrioridad>> ConsultarHistorialAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}
