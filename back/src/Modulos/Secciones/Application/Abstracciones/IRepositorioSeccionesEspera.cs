using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Abstracciones;

public interface IRepositorioSeccionesEspera
{
    Task DarBajaAsync(IngresoRetiradoSecciones retiro, CancellationToken tokenCancelacion = default);
    Task<T> ConOfertaProtegidaAsync<T>(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        Func<IReadOnlyList<Seccion>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}
