using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Persistencia;

public interface IRepositorioSorteos
{
    Task<Sorteo?> ObtenerVigenteAsync(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sorteo>> ListarVigentesAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);

    Task<int> ObtenerUltimaVersionAsync(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default);

    Task GuardarNuevaVersionAsync(
        Sorteo nuevo,
        Sorteo? anterior,
        CancellationToken cancellationToken = default);
}
