using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Abstracciones;

public interface IRepositorioDistribuciones
{
    Task<int> ObtenerUltimaVersionAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task AgregarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default);
    Task<PropuestaDistribucion?> ObtenerPorIdAsync(Guid propuestaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PropuestaDistribucion>> ListarPorPeriodoAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task ActualizarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default);
    Task<bool> ExisteAsignacionActivaAsync(Guid ingresoId, CancellationToken cancellationToken = default);
    Task ConfirmarAsync(
        PropuestaDistribucion propuesta,
        IReadOnlyList<AsignacionSeccion> asignaciones,
        CancellationToken cancellationToken = default);
}
