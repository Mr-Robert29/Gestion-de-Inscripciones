namespace Jardin.Inscripciones.Contracts;

// Dirección declarada para esta solicitud, independiente del jardín y de otras solicitudes.
public sealed record DatosDomicilioSolicitud(
    string Calle,
    string Numero,
    string Localidad,
    string Provincia,
    string? CodigoPostal = null,
    string? Complemento = null,
    string? Piso = null,
    string? Departamento = null,
    string? Barrio = null);
