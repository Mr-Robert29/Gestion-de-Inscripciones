using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class RepositorioSorteos : IRepositorioSorteos
{
    private readonly ContextoVacantes _contexto;

    public RepositorioSorteos(ContextoVacantes contexto)
    {
        _contexto = contexto;
    }

    public Task<Sorteo?> ObtenerVigenteAsync(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default)
        => _contexto.Sorteos.FirstOrDefaultAsync(
            x => x.PeriodoId == periodoId && x.Sala == sala && x.Vigente,
            cancellationToken);

    public async Task<IReadOnlyList<Sorteo>> ListarVigentesAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
        => await _contexto.Sorteos
            .Where(x => x.PeriodoId == periodoId && x.Vigente)
            .OrderBy(x => x.Sala)
            .ToListAsync(cancellationToken);

    public async Task<int> ObtenerUltimaVersionAsync(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default)
    {
        var ultima = await _contexto.Sorteos
            .Where(x => x.PeriodoId == periodoId && x.Sala == sala)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken);
        return ultima ?? 0;
    }

    public async Task GuardarNuevaVersionAsync(
        Sorteo nuevo,
        Sorteo? anterior,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        if (anterior is not null)
            anterior.Desactivar();

        await _contexto.Sorteos.AddAsync(nuevo, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }
}
