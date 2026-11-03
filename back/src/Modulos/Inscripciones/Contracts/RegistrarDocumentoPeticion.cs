namespace Jardin.Inscripciones.Contracts;

public sealed record RegistrarDocumentoPeticion(
    string Tipo,
    string Estado,
    int RevisionSolicitud,
    string Motivo,
    string? Referencia = null,
    string? Observacion = null);
