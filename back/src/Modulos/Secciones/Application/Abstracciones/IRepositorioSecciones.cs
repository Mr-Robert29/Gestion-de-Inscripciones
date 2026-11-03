using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Abstracciones;

public interface IRepositorioSecciones
{
    Task AgregarAsync(Seccion seccion, CancellationToken cancellationToken = default);
    Task<Seccion?> ObtenerPorIdAsync(Guid seccionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Seccion>> ListarPorPeriodoAsync(Guid periodoId, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Seccion seccion, CancellationToken cancellationToken = default);
}
