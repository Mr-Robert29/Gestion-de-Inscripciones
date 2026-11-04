using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Propuestas;

public interface IContextoPropuestaIngresos
{
    Task<EntradasPropuestaIngresos> LeerAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<T> ConPeriodoProtegidoAsync<T>(
        Guid periodoId,
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}
