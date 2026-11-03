namespace Jardin.Inscripciones.Contracts;

public sealed record DatosDocumentoSolicitud(
    Guid Id,
    string Tipo,
    string Estado,
    string? Referencia,
    string? Observacion,
    Guid UsuarioRegistroId,
    DateTimeOffset FechaRegistro,
    Guid? UsuarioRevisionId,
    DateTimeOffset? FechaRevision);
