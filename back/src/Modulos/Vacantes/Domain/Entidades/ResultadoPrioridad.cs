namespace Jardin.Vacantes.Domain.Entidades;

public sealed record ResultadoPrioridad(
    Guid SolicitudId,
    Guid AspiranteId,
    int? Sala,
    int? Posicion,
    bool RequiereRevision,
    string? MotivoRevision,
    IReadOnlyList<string> CriteriosAplicados,
    IReadOnlyList<string> Explicacion);
