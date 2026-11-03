namespace Jardin.Inscripciones.Contracts;

public sealed record CorregirCondicionPeticion(
    string Tipo,
    string ValorDeclarado,
    int RevisionSolicitud,
    string Motivo,
    string? EvidenciaReferencia = null);
