namespace Jardin.Inscripciones.Contracts;

// Una declaración nueva siempre comienza pendiente de revisión.
public sealed record RegistrarCondicionPeticion(
    string Tipo,
    string ValorDeclarado,
    int RevisionSolicitud,
    string Motivo,
    string? EvidenciaReferencia = null);
