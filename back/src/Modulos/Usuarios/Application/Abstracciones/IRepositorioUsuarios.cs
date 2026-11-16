using Jardin.Usuarios.Domain.Entidades;

namespace Jardin.Usuarios.Application.Abstracciones;

public interface IRepositorioUsuarios
{
    Task<UsuarioPersonal?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UsuarioPersonal?> ObtenerPorCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioPersonal>> ListarTodosAsync(CancellationToken cancellationToken = default);
    Task AgregarAsync(UsuarioPersonal usuario, CancellationToken cancellationToken = default);
    Task ActualizarAsync(UsuarioPersonal usuario, CancellationToken cancellationToken = default);
    Task ActualizarConAuditoriaAsync(UsuarioPersonal usuario, IReadOnlyCollection<EventoAuditoriaPersonal> eventos, CancellationToken cancellationToken = default);
    Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioPersonal>> ListarPorInstitucionAsync(Guid institucionId, CancellationToken cancellationToken = default);
    Task<IdentidadPersonal?> ObtenerIdentidadPorDniAsync(string dniNormalizado, CancellationToken cancellationToken = default);
    Task<IdentidadPersonal?> ObtenerIdentidadPorIdAsync(Guid identidadId, CancellationToken cancellationToken = default);
    Task AgregarIdentidadAsync(IdentidadPersonal identidad, CancellationToken cancellationToken = default);
    Task ActualizarIdentidadAsync(IdentidadPersonal identidad, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioPersonal>> ListarPorIdentidadAsync(Guid identidadId, CancellationToken cancellationToken = default);
    Task ConsolidarIdentidadesAsync(Guid identidadOrigenId, IdentidadPersonal destino, CancellationToken cancellationToken = default);
    Task RegistrarAuditoriaAsync(EventoAuditoriaPersonal evento, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventoAuditoriaPersonal>> ListarAuditoriaAsync(CancellationToken cancellationToken = default);
}
