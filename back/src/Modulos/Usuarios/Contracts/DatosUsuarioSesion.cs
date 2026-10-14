namespace Jardin.Usuarios.Contracts;

public sealed record DatosUsuarioSesion(
    Guid UsuarioId,
    string Nombre,
    string CorreoElectronico,
    bool Activo,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permisos,
    IReadOnlyList<DatosAccesoInstitucion> Accesos);
