namespace Jardin.Usuarios.Contracts;

public sealed record DatosAccesoInstitucion(
    Guid Id,
    Guid InstitucionId,
    string Rol);
