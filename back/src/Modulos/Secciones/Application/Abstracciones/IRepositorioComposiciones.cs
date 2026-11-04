using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Abstracciones;

public interface IRepositorioComposiciones
{
    Task<T> ConSeccionProtegidaAsync<T>(
        Guid periodoId,
        Guid seccionId,
        Func<Seccion, IReadOnlyList<Guid>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion);
    Task<IReadOnlyList<Guid>> GuardarAsync(CambioComposicionSeccion cambio, CancellationToken tokenCancelacion);
    Task<IReadOnlyList<CambioComposicionSeccion>> ConsultarHistorialAsync(Guid seccionId, CancellationToken tokenCancelacion);
}
