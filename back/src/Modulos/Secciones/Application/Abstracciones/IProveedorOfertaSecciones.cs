namespace Jardin.Secciones.Application.Abstracciones;

public interface IProveedorOfertaSecciones
{
    Task<OfertaSeccionConfigurada?> ObtenerAsync(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        CancellationToken cancellationToken = default);
}

public sealed record OfertaSeccionConfigurada(
    Guid PeriodoId,
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    IReadOnlyList<IReadOnlyList<int>> CombinacionesPermitidas);
