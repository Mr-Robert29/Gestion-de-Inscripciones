namespace Jardin.Inscripciones.Contracts;

public sealed record RegistrarSolicitudRespuesta(
    Guid Id,
    string EstadoRecepcion,
    int? Sala,
    bool RequiereRevisionEdad,
    int AlertasPosibleDuplicado = 0);
