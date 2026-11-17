namespace Jardin.Usuarios.Contracts;

public interface IModuloUsuarios
{
    Task<RespuestaAutenticacion?> IniciarSesionAsync(IniciarSesionPeticion peticion, CancellationToken cancellationToken = default);
    Task<RespuestaAutenticacion> CambiarClaveAsync(Guid usuarioId, CambiarClavePeticion peticion, CancellationToken cancellationToken = default);
    Task<DatosUsuarioSesion?> ObtenerSesionActualAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosUsuarioPersonal>> ListarUsuariosAsync(CancellationToken cancellationToken = default);
    Task<DatosUsuarioPersonal?> ObtenerUsuarioPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<DatosDniUsuario?> ObtenerDniUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Guid> CrearUsuarioAsync(CrearUsuarioPersonalPeticion peticion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosPersonalInstitucion>> ListarPersonalInstitucionAsync(Guid institucionId, CancellationToken cancellationToken = default);
    Task<Guid> CrearCuentaInstitucionAsync(Guid institucionId, CrearCuentaInstitucionPeticion peticion, string rol, Guid actorUsuarioId, CancellationToken cancellationToken = default);
    Task VincularDniAsync(Guid usuarioId, string dni, Guid actorUsuarioId, CancellationToken cancellationToken = default);
    Task RegistrarAuditoriaAsync(Guid actorUsuarioId, Guid? usuarioAfectadoId, Guid? institucionId, string accion, string detalle, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosAuditoriaPersonal>> ListarAuditoriaAsync(CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoUsuarioAsync(Guid usuarioId, bool activo, Guid actorUsuarioId, CancellationToken cancellationToken = default);
    Task<bool> AsignarAccesoInstitucionAsync(Guid usuarioId, AsignarAccesoInstitucionPeticion peticion, CancellationToken cancellationToken = default);
    Task<bool> RevocarAccesoInstitucionAsync(Guid usuarioId, Guid institucionId, Guid actorUsuarioId, CancellationToken cancellationToken = default);
}
