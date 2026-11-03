namespace Jardin.Secciones.Contracts;

public sealed record DatosPropuestaDistribucion(
    Guid Id,
    Guid PeriodoId,
    int Version,
    int RevisionIngresos,
    int RevisionSecciones,
    string Estado,
    DateTimeOffset FechaCreacion,
    Guid CreadaPor,
    Guid? ConfirmadaPor,
    DateTimeOffset? FechaConfirmacion,
    IReadOnlyList<DatosAsignacionPropuesta> Asignaciones);
