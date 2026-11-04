namespace Jardin.Vacantes.Contracts;

public sealed record DatosAceptacionTurno(
    Guid Id,
    Guid PropuestaId,
    Guid SolicitudId,
    Guid TurnoId,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string? Referencia);
