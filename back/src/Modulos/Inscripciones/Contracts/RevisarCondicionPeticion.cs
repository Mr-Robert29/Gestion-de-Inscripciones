namespace Jardin.Inscripciones.Contracts;

// Revisar no modifica el tipo ni el valor originalmente declarado.
public sealed record RevisarCondicionPeticion(
    string Estado,
    int RevisionSolicitud,
    string Motivo,
    string? EvidenciaReferencia = null,
    string? Observacion = null);
