using Jardin.Usuarios.Domain.Entidades;

namespace Jardin.Usuarios.Application.Abstracciones;

public interface IRepositorioUsuarios
{
    Task<UsuarioPersonal?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UsuarioPersonal?> ObtenerPorCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioPersonal>> ListarTodosAsync(CancellationToken cancellationToken = default);
    Task AgregarAsync(UsuarioPersonal usuario, CancellationToken cancellationToken = default);
    Task ActualizarAsync(UsuarioPersonal usuario, CancellationToken cancellationToken = default);
    Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default);
}
