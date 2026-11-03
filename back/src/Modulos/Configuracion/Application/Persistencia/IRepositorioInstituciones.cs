using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Persistencia;

// Institución e historial se guardan juntos en una única transacción.
public interface IRepositorioInstituciones
{
    Task<Institucion?> ObtenerAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<Institucion>> ListarAsync(CancellationToken tokenCancelacion = default);
    Task AgregarAsync(
        Institucion institucion,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    Task ActualizarAsync(
        Institucion institucion,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<CambioInstitucion>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task RegistrarUsoAsync(UsoUbicacion uso, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<UsoUbicacion>> ConsultarUsosAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
}
