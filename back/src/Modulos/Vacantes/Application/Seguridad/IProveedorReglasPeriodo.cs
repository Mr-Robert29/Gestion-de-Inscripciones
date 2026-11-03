namespace Jardin.Vacantes.Application.Seguridad;

public interface IProveedorReglasPeriodo
{
    Task<ReglasPeriodoVacantes> ConsultarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}

public sealed record ReglasPeriodoVacantes(
    Guid VersionReglasId,
    IReadOnlyList<int> OrdenSalas,
    IReadOnlyList<string> OrdenProcedimiento);
