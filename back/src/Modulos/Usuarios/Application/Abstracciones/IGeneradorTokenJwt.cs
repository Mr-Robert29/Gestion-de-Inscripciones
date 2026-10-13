namespace Jardin.Usuarios.Application.Abstracciones;

public interface IGeneradorTokenJwt
{
    (string Token, DateTimeOffset Expiracion) GenerarToken(
        Guid usuarioId,
        string correoElectronico,
        string nombre,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permisos,
        IReadOnlyList<Guid> institucionesPermitidas);
}
