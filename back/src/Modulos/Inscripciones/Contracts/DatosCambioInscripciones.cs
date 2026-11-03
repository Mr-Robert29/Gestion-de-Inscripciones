namespace Jardin.Inscripciones.Contracts;

public sealed record DatosCambioInscripciones(
    Guid Id,
    Guid UsuarioId,
    Guid EntidadId,
    DateTimeOffset Fecha,
    string Accion,
    string Datos);
