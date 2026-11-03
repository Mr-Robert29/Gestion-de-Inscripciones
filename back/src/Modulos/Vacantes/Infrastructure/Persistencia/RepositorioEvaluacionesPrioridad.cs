using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class RepositorioEvaluacionesPrioridad : IRepositorioEvaluacionesPrioridad
{
    private readonly ContextoVacantes _contexto;

    public RepositorioEvaluacionesPrioridad(ContextoVacantes contexto)
    {
        _contexto = contexto;
    }

    public async Task GuardarAsync(
        EvaluacionPrioridad evaluacion,
        CancellationToken cancellationToken = default)
    {
        await _contexto.EvaluacionesPrioridad.AddAsync(evaluacion, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }
}
