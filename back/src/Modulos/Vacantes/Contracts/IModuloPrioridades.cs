namespace Jardin.Vacantes.Contracts;

public interface IModuloPrioridades
{
    Task<IReadOnlyList<DatosPrioridad>> EvaluarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosEvaluacionPrioridad>> ConsultarHistorialAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}
