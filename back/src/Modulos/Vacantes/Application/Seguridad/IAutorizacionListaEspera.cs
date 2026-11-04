namespace Jardin.Vacantes.Application.Seguridad;

public interface IAutorizacionListaEspera
{
    Task<AutorizacionVacantes> AutorizarConsultaEsperaAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
