namespace Jardin.Inscripciones.Contracts;

// Reemplaza estos datos propios de la solicitud; no modifica al aspirante compartido.
public sealed record CorregirSolicitudPeticion(
    DatosDomicilioSolicitud Domicilio,
    Guid TurnoPreferidoId,
    IReadOnlyList<Guid> TurnosAlternativosAceptados,
    int RevisionSolicitud,
    string Motivo,
    string? ReferenciaConsentimiento = null);
