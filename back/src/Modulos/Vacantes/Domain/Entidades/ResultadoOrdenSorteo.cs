namespace Jardin.Vacantes.Domain.Entidades;

public sealed record ResultadoOrdenSorteo(
    Guid SolicitudId,
    Guid AspiranteId,
    int Sala,
    int? TerminacionDni,
    int? ClaveCircular,
    int? Posicion,
    bool RequiereRevision,
    string? MotivoRevision);
