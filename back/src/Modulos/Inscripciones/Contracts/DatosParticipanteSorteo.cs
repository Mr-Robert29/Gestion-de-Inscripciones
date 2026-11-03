namespace Jardin.Inscripciones.Contracts;

public sealed record DatosParticipanteSorteo(
    Guid SolicitudId,
    Guid AspiranteId,
    Guid InstitucionId,
    Guid PeriodoId,
    int Sala,
    string? Dni);
