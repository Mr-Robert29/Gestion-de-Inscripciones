namespace Jardin.Usuarios.Contracts;

public sealed record AsignarAccesoInstitucionPeticion(
    Guid InstitucionId,
    string Rol);
