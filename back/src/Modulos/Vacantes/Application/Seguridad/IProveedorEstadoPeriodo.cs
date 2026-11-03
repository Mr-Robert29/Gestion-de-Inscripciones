namespace Jardin.Vacantes.Application.Seguridad;

public interface IProveedorEstadoPeriodo
{
    Task<EstadoPeriodoVacantes> ConsultarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}

public sealed record EstadoPeriodoVacantes(
    Guid InstitucionId,
    string Estado,
    int Revision)
{
    public bool RecepcionCerrada => Estado == "RecepcionCerrada";
}
