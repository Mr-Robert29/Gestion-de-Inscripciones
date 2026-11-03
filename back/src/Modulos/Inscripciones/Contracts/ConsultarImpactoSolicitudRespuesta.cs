namespace Jardin.Inscripciones.Contracts;

// Solo informa referencias registradas: no descubre procesos externos que todavía no se integraron.
public sealed record ConsultarImpactoSolicitudRespuesta(
    Guid SolicitudId,
    int RevisionDatos,
    int RevisionDomicilio,
    IReadOnlyList<DatosUsoSolicitud> UsosDesactualizados);
