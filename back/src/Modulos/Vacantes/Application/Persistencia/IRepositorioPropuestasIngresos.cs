using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Persistencia;

public interface IRepositorioPropuestasIngresos
{
    Task GuardarAsync(PropuestaIngresos propuesta, CancellationToken tokenCancelacion = default);
    Task<PropuestaIngresos?> ObtenerAsync(Guid propuestaId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<PropuestaIngresos>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<OcupacionPlaza>> ListarOcupacionesAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
