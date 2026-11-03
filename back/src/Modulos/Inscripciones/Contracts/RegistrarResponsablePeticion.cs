namespace Jardin.Inscripciones.Contracts;

public sealed record RegistrarResponsablePeticion(
    string Nombre,
    string Apellido,
    string Telefono,
    string? Documento = null,
    string? TipoDocumento = null,
    string? CorreoElectronico = null);
