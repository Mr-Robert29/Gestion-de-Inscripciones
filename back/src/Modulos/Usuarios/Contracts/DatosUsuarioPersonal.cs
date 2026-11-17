namespace Jardin.Usuarios.Contracts;

public sealed record DatosUsuarioPersonal(
    Guid Id,
    string Nombre,
    string CorreoElectronico,
    bool Activo,
    IReadOnlyList<string> RolesGlobales,
    IReadOnlyList<DatosAccesoInstitucion> Accesos,
    Guid? IdentidadPersonalId = null,
    string? DniEnmascarado = null);
