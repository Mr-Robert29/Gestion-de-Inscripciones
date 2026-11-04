namespace Jardin.Vacantes.Contracts;

public sealed record DatosCasoRevision(
    Guid Id,
    Guid PeriodoId,
    int Sala,
    string Tipo,
    string Estado,
    IReadOnlyList<Guid> SolicitudesAfectadas,
    IReadOnlyList<Guid> OrdenDecidido,
    Guid? SorteoId,
    int? VersionSorteo,
    Guid? VersionReglasId,
    Guid? UsuarioResolucion,
    DateTimeOffset? FechaResolucion,
    string? MotivoResolucion,
    DateTimeOffset FechaCreacion,
    int Revision);
