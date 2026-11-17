namespace Jardin.Usuarios.Contracts;

public sealed record DatosPersonalInstitucion(
    Guid UsuarioId,
    string Nombre,
    string CorreoElectronico,
    bool Activo,
    string Rol);
