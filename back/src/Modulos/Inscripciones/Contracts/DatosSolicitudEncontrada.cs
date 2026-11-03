namespace Jardin.Inscripciones.Contracts;

public sealed record DatosSolicitudEncontrada(
    Guid Id,
    Guid PeriodoId,
    Guid AspiranteId,
    string Nombre,
    string Apellido,
    string? Dni,
    int? Sala,
    string EstadoRecepcion,
    int RevisionDatos);
