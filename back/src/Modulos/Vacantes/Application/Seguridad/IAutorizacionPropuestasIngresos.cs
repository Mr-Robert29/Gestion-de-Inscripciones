namespace Jardin.Vacantes.Application.Seguridad;

public interface IAutorizacionPropuestasIngresos
{
    Task<AutorizacionVacantes> AutorizarPropuestaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
