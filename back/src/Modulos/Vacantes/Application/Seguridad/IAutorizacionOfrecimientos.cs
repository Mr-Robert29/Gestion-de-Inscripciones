namespace Jardin.Vacantes.Application.Seguridad;

public interface IAutorizacionOfrecimientos
{
    Task<AutorizacionVacantes> AutorizarOfertaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<AutorizacionVacantes> AutorizarRespuestaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<AutorizacionVacantes> AutorizarConfirmacionEsperaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<AutorizacionVacantes> AutorizarBajaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
