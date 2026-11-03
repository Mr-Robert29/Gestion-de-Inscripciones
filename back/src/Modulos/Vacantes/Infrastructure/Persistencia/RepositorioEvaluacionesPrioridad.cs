using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<EvaluacionPrioridad>> ListarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
        => await _contexto.EvaluacionesPrioridad
            .AsNoTracking()
            .Where(x => x.PeriodoId == periodoId)
            .OrderByDescending(x => x.Fecha)
            .ToArrayAsync(cancellationToken);
}
