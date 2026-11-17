namespace Jardin.Usuarios.Contracts;

public sealed record CrearPersonalInstitucionRootPeticion(
    Guid InstitucionId,
    string Rol,
    string Nombre,
    string CorreoElectronico,
    string ClaveInicial,
    string Dni);
