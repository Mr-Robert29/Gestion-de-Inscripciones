namespace Jardin.Inscripciones.Contracts;

// Sin decisión para la dirección actual, el estado es PendienteRevision y RevisionActual es null.
public sealed record ConsultarRevisionDomicilioRespuesta(
    Guid SolicitudId,
    int RevisionSolicitud,
    int RevisionDomicilio,
    string Estado,
    int UltimoNumeroRegistro,
    DatosRevisionDomicilio? RevisionActual,
    IReadOnlyList<DatosRevisionDomicilio> Historial);
