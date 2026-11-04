using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Persistencia;

public interface IRepositorioCasosRevision
{
    Task<CasoRevision?> ObtenerAsync(Guid casoId, CancellationToken cancellationToken = default);
    Task<CasoRevision?> ObtenerPorHuellaAsync(Guid periodoId, int sala, TipoCasoRevision tipo, string huellaOrigen, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CasoRevision>> ListarAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task GuardarAsync(CasoRevision caso, CancellationToken cancellationToken = default);
}
