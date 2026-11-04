namespace Jardin.Vacantes.Application.Seguridad;

public interface IAutorizacionPropuestasIngresos
{
    Task<AutorizacionVacantes> AutorizarPropuestaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<AutorizacionVacantes> AutorizarModificacionAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<AutorizacionVacantes> AutorizarAceptacionTurnoAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<AutorizacionVacantes> AutorizarConfirmacionAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
