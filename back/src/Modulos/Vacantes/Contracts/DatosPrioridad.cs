namespace Jardin.Vacantes.Contracts;

public sealed record DatosPrioridad(
    Guid SolicitudId,
    Guid AspiranteId,
    int Sala,
    int? Posicion,
    bool RequiereRevision,
    string? MotivoRevision,
    IReadOnlyList<string> CriteriosAplicados,
    IReadOnlyList<string> Explicacion,
    Guid VersionReglasId);
