namespace Jardin.Inscripciones.Contracts;

public sealed record ConsultarResponsableRespuesta(
    Guid Id,
    string Nombre,
    string Apellido,
    string Telefono,
    string? Documento,
    string? TipoDocumento,
    string? CorreoElectronico);
