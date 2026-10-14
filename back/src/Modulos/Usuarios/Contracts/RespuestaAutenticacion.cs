namespace Jardin.Usuarios.Contracts;

public sealed record RespuestaAutenticacion(
    string Token,
    DateTimeOffset Expiracion,
    Guid UsuarioId,
    string Nombre,
    string CorreoElectronico,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> InstitucionesPermitidas);
