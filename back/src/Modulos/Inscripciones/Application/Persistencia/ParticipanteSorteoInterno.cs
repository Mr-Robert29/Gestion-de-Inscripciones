namespace Jardin.Inscripciones.Application.Persistencia;

public sealed record ParticipanteSorteoInterno(
    Guid SolicitudId,
    Guid AspiranteId,
    int Sala,
    string? Dni);
