using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Persistencia;

public interface IRepositorioVersionesConfiguracion
{
    Task<VersionConfiguracion?> ObtenerAsync(Guid id, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<VersionConfiguracion>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task AgregarAsync(
        VersionConfiguracion version,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
}
