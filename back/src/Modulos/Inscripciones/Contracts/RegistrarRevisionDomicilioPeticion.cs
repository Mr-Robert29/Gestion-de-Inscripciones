namespace Jardin.Inscripciones.Contracts;

public sealed record RegistrarRevisionDomicilioPeticion(
    string Estado,
    int RevisionSolicitud,
    int RevisionDomicilio,
    int UltimoNumeroRegistro,
    string Motivo,
    string? Referencia = null);
