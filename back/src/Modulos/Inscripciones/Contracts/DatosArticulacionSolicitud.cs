namespace Jardin.Inscripciones.Contracts;

public sealed record DatosArticulacionSolicitud(
    Guid SolicitudId,
    DatosCondicionSolicitud Condicion,
    int RevisionSolicitud);
