namespace Jardin.Vacantes.Application.Seguridad;

public interface IAutorizacionNominaDistrito
{
    Task<AutorizacionVacantes> AutorizarConsultaAsync(
        Guid periodoId,
        CancellationToken tokenCancelacion = default);
}
