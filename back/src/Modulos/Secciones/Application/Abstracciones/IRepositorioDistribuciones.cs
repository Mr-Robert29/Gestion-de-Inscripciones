using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Abstracciones;

public interface IRepositorioDistribuciones
{
    Task<int> ObtenerUltimaVersionAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task AgregarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default);
    Task<PropuestaDistribucion?> ObtenerPorIdAsync(Guid propuestaId, CancellationToken cancellationToken = default);
    Task ActualizarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default);
}
