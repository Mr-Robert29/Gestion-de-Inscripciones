namespace Jardin.Inscripciones.Contracts;

// Entrada de HU-06: no contiene domicilio ni datos de una solicitud.
public sealed record RegistrarAspirantePeticion(
    string Nombre,
    string Apellido,
    DateOnly FechaNacimiento,
    string? Dni = null,
    string? TipoDocumento = null,
    string? Sexo = null);
