namespace Jardin.Usuarios.Contracts;

public sealed record DatosUsuarioPersonal(
    Guid Id,
    string Nombre,
    string CorreoElectronico,
    bool Activo,
    IReadOnlyList<DatosAccesoInstitucion> Accesos);
