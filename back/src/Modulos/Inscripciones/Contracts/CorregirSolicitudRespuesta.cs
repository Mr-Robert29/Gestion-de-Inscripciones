namespace Jardin.Inscripciones.Contracts;

public sealed record CorregirSolicitudRespuesta(
    Guid Id,
    int RevisionSolicitud,
    int RevisionDomicilio,
    bool CambioDomicilio);
