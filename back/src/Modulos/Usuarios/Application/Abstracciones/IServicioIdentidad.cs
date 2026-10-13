namespace Jardin.Usuarios.Application.Abstracciones;

public interface IServicioIdentidad
{
    Task<(bool Exito, string? Error)> CrearCuentaAsync(
        Guid usuarioId,
        string correoElectronico,
        string clave,
        string rolGlobal,
        CancellationToken cancellationToken = default);

    Task<(bool Valido, Guid UsuarioId, IReadOnlyList<string> Roles)> ValidarCredencialesAsync(
        string correoElectronico,
        string clave,
        CancellationToken cancellationToken = default);
}
