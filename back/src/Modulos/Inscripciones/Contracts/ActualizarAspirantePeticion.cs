namespace Jardin.Inscripciones.Contracts;

public sealed record ActualizarAspirantePeticion(
    Guid SolicitudId,
    int RevisionSolicitud,
    string Nombre,
    string Apellido,
    DateOnly FechaNacimiento,
    string? Dni,
    string? TipoDocumento,
    string? Sexo);
