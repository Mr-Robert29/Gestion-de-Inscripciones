namespace Jardin.Vacantes.Contracts;

public sealed record DatosOrdenSorteo(
    Guid SorteoId,
    int VersionSorteo,
    Guid SolicitudId,
    Guid AspiranteId,
    int Sala,
    int? TerminacionDni,
    int? ClaveCircular,
    int? Posicion,
    bool RequiereRevision,
    string? MotivoRevision);
