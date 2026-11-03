using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Persistencia;

public interface IRepositorioEvaluacionesPrioridad
{
    Task GuardarAsync(
        EvaluacionPrioridad evaluacion,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvaluacionPrioridad>> ListarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}
