using Jardin.Usuarios.Application.Abstracciones;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Usuarios.Domain.Entidades;

namespace Jardin.Usuarios.Application.Servicios;

public sealed class ServicioUsuarios : IModuloUsuarios
{
    private readonly IRepositorioUsuarios _repositorioUsuarios;
    private readonly IServicioIdentidad _servicioIdentidad;
    private readonly IGeneradorTokenJwt _generadorTokenJwt;

    public ServicioUsuarios(
        IRepositorioUsuarios repositorioUsuarios,
        IServicioIdentidad servicioIdentidad,
        IGeneradorTokenJwt generadorTokenJwt)
    {
        _repositorioUsuarios = repositorioUsuarios;
        _servicioIdentidad = servicioIdentidad;
        _generadorTokenJwt = generadorTokenJwt;
    }

    public async Task<RespuestaAutenticacion?> IniciarSesionAsync(
        IniciarSesionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var (valido, usuarioId, roles) = await _servicioIdentidad.ValidarCredencialesAsync(
            peticion.CorreoElectronico,
            peticion.Clave,
            cancellationToken);

        if (!valido)
            return null;

        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null || !usuario.Activo)
            return null;

        var rolesTotales = new HashSet<string>(roles);
        foreach (var acceso in usuario.Accesos)
        {
            rolesTotales.Add(acceso.Rol);
        }

        var permisos = new HashSet<string>();
        foreach (var rol in rolesTotales)
        {
            foreach (var permiso in PermisosPersonal.ObtenerPermisosPorRol(rol))
            {
                permisos.Add(permiso);
            }
        }

        var institucionesPermitidas = usuario.Accesos
            .Select(a => a.InstitucionId)
            .Distinct()
            .ToList();

        var (token, expiracion) = _generadorTokenJwt.GenerarToken(
            usuario.Id,
            usuario.CorreoElectronico,
            usuario.Nombre,
            rolesTotales.ToList(),
            permisos.ToList(),
            institucionesPermitidas);

        return new RespuestaAutenticacion(
            token,
            expiracion,
            usuario.Id,
            usuario.Nombre,
            usuario.CorreoElectronico,
            rolesTotales.ToList(),
            institucionesPermitidas);
    }

    public async Task<DatosUsuarioSesion?> ObtenerSesionActualAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null || !usuario.Activo)
            return null;

        var rolesTotales = new HashSet<string>();
        foreach (var acceso in usuario.Accesos)
        {
            rolesTotales.Add(acceso.Rol);
        }

        var permisos = new HashSet<string>();
        foreach (var rol in rolesTotales)
        {
            foreach (var permiso in PermisosPersonal.ObtenerPermisosPorRol(rol))
            {
                permisos.Add(permiso);
            }
        }

        var accesosDto = usuario.Accesos
            .Select(a => new DatosAccesoInstitucion(a.Id, a.InstitucionId, a.Rol))
            .ToList();

        return new DatosUsuarioSesion(
            usuario.Id,
            usuario.Nombre,
            usuario.CorreoElectronico,
            usuario.Activo,
            rolesTotales.ToList(),
            permisos.ToList(),
            accesosDto);
    }

    public async Task<IReadOnlyList<DatosUsuarioPersonal>> ListarUsuariosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await _repositorioUsuarios.ListarTodosAsync(cancellationToken);
        return usuarios
            .Select(MapearADatosUsuarioPersonal)
            .ToList();
    }

    public async Task<DatosUsuarioPersonal?> ObtenerUsuarioPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        return usuario is null ? null : MapearADatosUsuarioPersonal(usuario);
    }

    public async Task<Guid> CrearUsuarioAsync(
        CrearUsuarioPersonalPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.CorreoElectronico);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.ClaveInicial);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.RolGlobal);

        var rolLimpio = peticion.RolGlobal.Trim();
        if (!RolesPersonal.Todos.Contains(rolLimpio))
            throw new ArgumentException($"El rol '{peticion.RolGlobal}' no es válido.", nameof(peticion));

        var correoLimpio = peticion.CorreoElectronico.Trim();
        if (await _repositorioUsuarios.ExisteCorreoAsync(correoLimpio, cancellationToken))
            throw new InvalidOperationException($"Ya existe un usuario registrado con el correo '{correoLimpio}'.");

        var usuarioId = Guid.NewGuid();
        var usuario = new UsuarioPersonal(usuarioId, peticion.Nombre.Trim(), correoLimpio);

        if (peticion.InstitucionInicialId.HasValue && peticion.InstitucionInicialId.Value != Guid.Empty)
        {
            usuario.AsignarAcceso(peticion.InstitucionInicialId.Value, rolLimpio);
        }

        var (exito, error) = await _servicioIdentidad.CrearCuentaAsync(
            usuarioId,
            correoLimpio,
            peticion.ClaveInicial,
            rolLimpio,
            cancellationToken);

        if (!exito)
            throw new InvalidOperationException($"No se pudo crear la cuenta de identidad: {error}");

        await _repositorioUsuarios.AgregarAsync(usuario, cancellationToken);
        return usuarioId;
    }

    public async Task<bool> CambiarEstadoUsuarioAsync(
        Guid usuarioId,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return false;

        if (activo)
            usuario.Activar();
        else
            usuario.Desactivar();

        await _repositorioUsuarios.ActualizarAsync(usuario, cancellationToken);
        return true;
    }

    public async Task<bool> AsignarAccesoInstitucionAsync(
        Guid usuarioId,
        AsignarAccesoInstitucionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return false;

        usuario.AsignarAcceso(peticion.InstitucionId, peticion.Rol);
        await _repositorioUsuarios.ActualizarAsync(usuario, cancellationToken);
        return true;
    }

    public async Task<bool> RevocarAccesoInstitucionAsync(
        Guid usuarioId,
        Guid institucionId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return false;

        var resultado = usuario.RevocarAcceso(institucionId);
        if (!resultado)
            return false;

        await _repositorioUsuarios.ActualizarAsync(usuario, cancellationToken);
        return true;
    }

    private static DatosUsuarioPersonal MapearADatosUsuarioPersonal(UsuarioPersonal usuario)
    {
        var accesosDto = usuario.Accesos
            .Select(a => new DatosAccesoInstitucion(a.Id, a.InstitucionId, a.Rol))
            .ToList();

        return new DatosUsuarioPersonal(
            usuario.Id,
            usuario.Nombre,
            usuario.CorreoElectronico,
            usuario.Activo,
            accesosDto);
    }
}
