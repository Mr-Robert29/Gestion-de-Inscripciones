namespace Jardin.Api.Reportes;

public interface IConsultaReportes
{
    Task<ConsultarReportesRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default);
}
