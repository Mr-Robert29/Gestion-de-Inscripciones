using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class RepositorioCasosRevision : IRepositorioCasosRevision
{
    private readonly ContextoVacantes _contexto;

    public RepositorioCasosRevision(ContextoVacantes contexto)
    {
        _contexto = contexto;
    }

    public Task<CasoRevision?> ObtenerAsync(Guid casoId, CancellationToken cancellationToken = default)
        => _contexto.CasosRevision.SingleOrDefaultAsync(x => x.Id == casoId, cancellationToken);

    public Task<CasoRevision?> ObtenerPorHuellaAsync(
        Guid periodoId,
        int sala,
        TipoCasoRevision tipo,
        string huellaOrigen,
        CancellationToken cancellationToken = default)
        => _contexto.CasosRevision.SingleOrDefaultAsync(
            x => x.PeriodoId == periodoId && x.Sala == sala && x.Tipo == tipo && x.HuellaOrigen == huellaOrigen,
            cancellationToken);

    public async Task<IReadOnlyList<CasoRevision>> ListarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
        => (await _contexto.CasosRevision.AsNoTracking()
            .Where(x => x.PeriodoId == periodoId)
            .ToArrayAsync(cancellationToken)).OrderBy(x => x.Sala)
            .ThenBy(x => x.Estado)
            .ThenBy(x => x.FechaCreacion)
            .ToArray();

    public async Task<IReadOnlyList<HistorialCasoRevision>> ListarHistorialAsync(
        Guid casoId,
        CancellationToken cancellationToken = default)
        => await _contexto.CambiosCasosRevision.AsNoTracking()
            .Where(x => x.CasoRevisionId == casoId)
            .OrderBy(x => x.Fecha)
            .ToArrayAsync(cancellationToken);

    public async Task GuardarAsync(CasoRevision caso, CancellationToken cancellationToken = default)
    {
        if (_contexto.Entry(caso).State == EntityState.Detached)
            _contexto.CasosRevision.Add(caso);

        try
        {
            await _contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("El caso cambió mientras se lo estaba editando.");
        }
    }

    public async Task GuardarConHistorialAsync(
        CasoRevision caso,
        HistorialCasoRevision historial,
        CancellationToken cancellationToken = default)
    {
        _contexto.CambiosCasosRevision.Add(historial);
        try
        {
            await _contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("El caso cambió mientras se lo estaba editando.");
        }
    }
}
