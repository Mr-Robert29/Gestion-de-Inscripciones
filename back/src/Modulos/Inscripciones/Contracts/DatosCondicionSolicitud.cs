namespace Jardin.Inscripciones.Contracts;

public sealed record DatosCondicionSolicitud(
    Guid Id,
    string Tipo,
    string ValorDeclarado,
    string EstadoVerificacion,
    string? EvidenciaReferencia,
    string? Observacion,
    Guid UsuarioRegistroId,
    DateTimeOffset FechaRegistro,
    Guid? UsuarioRevisionId,
    DateTimeOffset? FechaRevision);
