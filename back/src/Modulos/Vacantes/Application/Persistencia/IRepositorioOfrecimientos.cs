using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Persistencia;

public interface IRepositorioOfrecimientos
{
    Task<IngresoConfirmado?> ObtenerIngresoAsync(Guid ingresoId, CancellationToken tokenCancelacion = default);
    Task GuardarSolicitudBajaAsync(IngresoConfirmado ingreso, CancellationToken tokenCancelacion = default);
    Task<IngresoConfirmado> CompletarBajaAsync(Guid ingresoId, CancellationToken tokenCancelacion = default);
    Task<OfrecimientoVacante?> ObtenerAsync(Guid ofrecimientoId, CancellationToken tokenCancelacion = default);
    Task<OfrecimientoVacante?> BuscarOperacionAsync(Guid periodoId, Guid operacionId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<OfrecimientoVacante>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<CambioOfrecimiento>> HistorialAsync(Guid ofrecimientoId, CancellationToken tokenCancelacion = default);
    Task CrearAsync(
        OfrecimientoVacante ofrecimiento,
        OcupacionPlaza reserva,
        CancellationToken tokenCancelacion = default);
    Task GuardarRespuestaAsync(
        OfrecimientoVacante ofrecimiento,
        CambioOfrecimiento cambio,
        CancellationToken tokenCancelacion = default);
    Task ConfirmarAsync(
        OfrecimientoVacante ofrecimiento,
        IngresoConfirmado ingreso,
        CambioOfrecimiento cambio,
        CancellationToken tokenCancelacion = default);
}
