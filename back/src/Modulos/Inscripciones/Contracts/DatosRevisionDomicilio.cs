namespace Jardin.Inscripciones.Contracts;

// EsActual requiere la versión actual del domicilio y ser la última decisión registrada.
public sealed record DatosRevisionDomicilio(
    Guid Id,
    int NumeroRegistro,
    int RevisionDomicilio,
    int RevisionSolicitudUtilizada,
    string Estado,
    DatosDomicilioSolicitud Domicilio,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string Motivo,
    string? Referencia,
    bool EsActual);
