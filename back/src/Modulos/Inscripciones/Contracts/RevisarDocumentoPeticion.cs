namespace Jardin.Inscripciones.Contracts;

public sealed record RevisarDocumentoPeticion(
    string Estado,
    int RevisionSolicitud,
    string Motivo,
    string? Referencia = null,
    string? Observacion = null);
