namespace Jardin.Inscripciones.Contracts;

public sealed record DatosUsoSolicitud(
    Guid ReferenciaId,
    string Tipo,
    int RevisionDatos,
    int RevisionDomicilio);
