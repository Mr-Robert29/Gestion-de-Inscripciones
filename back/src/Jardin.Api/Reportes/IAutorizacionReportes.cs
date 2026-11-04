using Jardin.Vacantes.Application.Seguridad;

namespace Jardin.Api.Reportes;

public interface IAutorizacionReportes
{
    Task<AutorizacionVacantes> AutorizarConsultaReportesAsync(
        Guid periodoId,
        CancellationToken tokenCancelacion = default);
}
