namespace Jardin.Vacantes.Application.Seguridad;

public interface IAutorizacionVacantes
{
    Task<AutorizacionVacantes> AutorizarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);

    Task<AutorizacionVacantes> AutorizarResolucionAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);

    Task<AutorizacionVacantes> AutorizarConsultaReportesAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}

public sealed record AutorizacionVacantes(
    Guid UsuarioId,
    Guid InstitucionId);
