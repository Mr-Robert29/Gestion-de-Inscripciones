namespace Jardin.Usuarios.Contracts;

public interface IModuloUsuarios
{
    Task<RespuestaAutenticacion?> IniciarSesionAsync(IniciarSesionPeticion peticion, CancellationToken cancellationToken = default);
    Task<DatosUsuarioSesion?> ObtenerSesionActualAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosUsuarioPersonal>> ListarUsuariosAsync(CancellationToken cancellationToken = default);
    Task<DatosUsuarioPersonal?> ObtenerUsuarioPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Guid> CrearUsuarioAsync(CrearUsuarioPersonalPeticion peticion, CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoUsuarioAsync(Guid usuarioId, bool activo, CancellationToken cancellationToken = default);
    Task<bool> AsignarAccesoInstitucionAsync(Guid usuarioId, AsignarAccesoInstitucionPeticion peticion, CancellationToken cancellationToken = default);
    Task<bool> RevocarAccesoInstitucionAsync(Guid usuarioId, Guid institucionId, CancellationToken cancellationToken = default);
}
