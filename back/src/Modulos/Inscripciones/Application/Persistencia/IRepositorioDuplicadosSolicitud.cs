using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.Servicios;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioDuplicadosSolicitud
{
    Task<DetectarDuplicadosRespuesta> DetectarAsync(
        Guid institucionId,
        Guid periodoId,
        Guid autor,
        CancellationToken tokenCancelacion = default);
    Task<PosibleDuplicado?> ObtenerAsync(
        Guid institucionId,
        Guid alertaId,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<PosibleDuplicado>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        EstadoPosibleDuplicado? estado,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<IdentificacionSolicitud>> LeerParejaActualAsync(PosibleDuplicado alerta, CancellationToken tokenCancelacion = default);
    Task GuardarResolucionAsync(
        PosibleDuplicado alerta,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
}
