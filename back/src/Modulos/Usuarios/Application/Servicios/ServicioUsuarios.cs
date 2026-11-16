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

        if (usuario.Accesos.Count == 0 && !roles.Contains(RolesPersonal.AdministradorDelSistema))
            return null;

        var rolesTotales = new HashSet<string>(roles.Where(r => r == RolesPersonal.AdministradorDelSistema));
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
            institucionesPermitidas,
            usuario.DebeCambiarClave);
    }

    public async Task<RespuestaAutenticacion> CambiarClaveAsync(
        Guid usuarioId,
        CambiarClavePeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.ClaveActual);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.ClaveNueva);
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró la cuenta.");
        if (!usuario.DebeCambiarClave)
            throw new InvalidOperationException("Esta cuenta no requiere un cambio inicial de contraseña.");
        var cambio = await _servicioIdentidad.CambiarClaveAsync(
            usuarioId,
            peticion.ClaveActual,
            peticion.ClaveNueva,
            cancellationToken);
        if (!cambio.Exito) throw new ArgumentException(cambio.Error ?? "No se pudo cambiar la contraseña.");
        usuario.MarcarClaveActualizada();
        await _repositorioUsuarios.ActualizarAsync(usuario, cancellationToken);
        return await IniciarSesionAsync(new IniciarSesionPeticion(usuario.CorreoElectronico, peticion.ClaveNueva), cancellationToken)
            ?? throw new InvalidOperationException("La contraseña se cambió, pero no se pudo renovar la sesión.");
    }

    public async Task<DatosUsuarioSesion?> ObtenerSesionActualAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null || !usuario.Activo)
            return null;

        var rolesGlobales = (await _servicioIdentidad.ObtenerRolesAsync(usuarioId, cancellationToken))
            .Where(r => r == RolesPersonal.AdministradorDelSistema).ToArray();
        var rolesTotales = new HashSet<string>(rolesGlobales);
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
            accesosDto,
            usuario.DebeCambiarClave);
    }

    public async Task<IReadOnlyList<DatosUsuarioPersonal>> ListarUsuariosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await _repositorioUsuarios.ListarTodosAsync(cancellationToken);
        var resultados = new List<DatosUsuarioPersonal>(usuarios.Count);
        foreach (var usuario in usuarios)
        {
            var rolesGlobales = (await _servicioIdentidad.ObtenerRolesAsync(usuario.Id, cancellationToken))
                .Where(r => r == RolesPersonal.AdministradorDelSistema).ToArray();
            resultados.Add(MapearADatosUsuarioPersonal(usuario, rolesGlobales));
        }
        return resultados;
    }

    public async Task<DatosUsuarioPersonal?> ObtenerUsuarioPorIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null) return null;
        var rolesGlobales = (await _servicioIdentidad.ObtenerRolesAsync(usuario.Id, cancellationToken))
            .Where(r => r == RolesPersonal.AdministradorDelSistema).ToArray();
        return MapearADatosUsuarioPersonal(usuario, rolesGlobales);
    }

    public async Task<DatosDniUsuario?> ObtenerDniUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        return usuario is null
            ? null
            : new DatosDniUsuario(usuario.IdentidadPersonal?.DniNormalizado);
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
        if (rolLimpio != RolesPersonal.AdministradorDelSistema || peticion.InstitucionInicialId.HasValue)
            throw new ArgumentException("Root solo puede crear cuentas globales de Administrador del sistema. Los roles del personal se asignan por institución.", nameof(peticion));

        var correoLimpio = peticion.CorreoElectronico.Trim().ToLowerInvariant();
        if (await _repositorioUsuarios.ExisteCorreoAsync(correoLimpio, cancellationToken))
            throw new InvalidOperationException($"Ya existe un usuario registrado con el correo '{correoLimpio}'.");

        var dni = IdentidadPersonal.NormalizarDni(peticion.Dni)
            ?? throw new ArgumentException("El DNI de la cuenta Root es obligatorio.", nameof(peticion));

        var usuarioId = Guid.NewGuid();
        var identidad = await _repositorioUsuarios.ObtenerIdentidadPorDniAsync(dni, cancellationToken);
        if (identidad is null)
        {
            identidad = new IdentidadPersonal(Guid.NewGuid(), dni);
            await _repositorioUsuarios.AgregarIdentidadAsync(identidad, cancellationToken);
        }
        var usuario = new UsuarioPersonal(usuarioId, peticion.Nombre.Trim(), correoLimpio, identidad.Id);

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

    public async Task<IReadOnlyList<DatosPersonalInstitucion>> ListarPersonalInstitucionAsync(
        Guid institucionId,
        CancellationToken cancellationToken = default)
    {
        var usuarios = await _repositorioUsuarios.ListarPorInstitucionAsync(institucionId, cancellationToken);
        return usuarios.Select(u =>
        {
            var acceso = u.Accesos.First(a => a.InstitucionId == institucionId);
            return new DatosPersonalInstitucion(u.Id, u.Nombre, u.CorreoElectronico, u.Activo, acceso.Rol);
        }).ToList();
    }

    public async Task<Guid> CrearCuentaInstitucionAsync(
        Guid institucionId,
        CrearCuentaInstitucionPeticion peticion,
        string rol,
        Guid actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.CorreoElectronico);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.ClaveInicial);
        ArgumentException.ThrowIfNullOrWhiteSpace(rol);
        rol = rol.Trim();
        if (!RolesPersonal.Institucionales.Contains(rol))
            throw new ArgumentException("El rol debe ser Administrativo o Directivo.", nameof(rol));
        var correo = peticion.CorreoElectronico.Trim().ToLowerInvariant();
        var dni = IdentidadPersonal.NormalizarDni(peticion.Dni)
            ?? throw new ArgumentException("El DNI es obligatorio para crear una cuenta institucional.", nameof(peticion));
        var identidad = await _repositorioUsuarios.ObtenerIdentidadPorDniAsync(dni, cancellationToken);
        if (identidad is not null && (await _repositorioUsuarios.ListarPorIdentidadAsync(identidad.Id, cancellationToken))
            .Any(u => u.Accesos.Any(a => a.InstitucionId == institucionId)))
            throw new InvalidOperationException("Ya existe una cuenta de esta persona en la institución. Revisá el correo o consultá con Root antes de crear otra.");
        var existente = await _repositorioUsuarios.ObtenerPorCorreoAsync(correo, cancellationToken);
        if (existente is not null)
        {
            if (identidad is null || existente.IdentidadPersonalId != identidad.Id || existente.Accesos.Any(a => a.InstitucionId == institucionId))
                throw new InvalidOperationException("No se pudo completar el alta. Verificá el correo y consultá con Root si ya existe una cuenta para esos datos.");
            existente.AsignarAcceso(institucionId, rol);
            await _repositorioUsuarios.ActualizarAsync(existente, cancellationToken);
            await RegistrarAuditoriaAsync(actorUsuarioId, existente.Id, institucionId, "Cuenta vinculada", $"Se agregó acceso con rol {rol} a una cuenta existente con el mismo DNI.", cancellationToken);
            return existente.Id;
        }

        var identidadNueva = identidad is null;
        identidad ??= new IdentidadPersonal(Guid.NewGuid(), dni);
        if (identidadNueva)
            await _repositorioUsuarios.AgregarIdentidadAsync(identidad, cancellationToken);

        var usuarioId = Guid.NewGuid();
        var usuario = new UsuarioPersonal(usuarioId, peticion.Nombre.Trim(), correo, identidad.Id, debeCambiarClave: true);
        usuario.AsignarAcceso(institucionId, rol);
        var cuenta = await _servicioIdentidad.CrearCuentaAsync(usuarioId, correo, peticion.ClaveInicial, null, cancellationToken);
        if (!cuenta.Exito)
            throw new InvalidOperationException(cuenta.Error ?? "No se pudo crear la cuenta.");
        await _repositorioUsuarios.AgregarAsync(usuario, cancellationToken);
        await RegistrarAuditoriaAsync(actorUsuarioId, usuario.Id, institucionId, "Cuenta creada", $"Se creó una cuenta institucional con rol {rol}.", cancellationToken);
        return usuarioId;
    }

    public async Task VincularDniAsync(Guid usuarioId, string dni, Guid actorUsuarioId, CancellationToken cancellationToken = default)
    {
        var dniNormalizado = IdentidadPersonal.NormalizarDni(dni)
            ?? throw new ArgumentException("Ingresá un DNI válido.", nameof(dni));
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró la cuenta.");
        var identidadOrigenId = usuario.IdentidadPersonalId
            ?? throw new InvalidOperationException("La cuenta no tiene una identidad vinculada.");
        var origen = usuario.IdentidadPersonal
            ?? await _repositorioUsuarios.ObtenerIdentidadPorIdAsync(identidadOrigenId, cancellationToken)
            ?? throw new InvalidOperationException("No se encontró la identidad vinculada.");
        if (origen.DniNormalizado is not null && origen.DniNormalizado != dniNormalizado)
            throw new InvalidOperationException("Esta identidad ya tiene otro DNI asignado.");
        var destino = await _repositorioUsuarios.ObtenerIdentidadPorDniAsync(dniNormalizado, cancellationToken);
        if (destino is null || destino.Id == origen.Id)
        {
            origen.AsignarDni(dniNormalizado);
            await _repositorioUsuarios.ActualizarIdentidadAsync(origen, cancellationToken);
            await RegistrarAuditoriaAsync(actorUsuarioId, usuarioId, null, "DNI vinculado", "Root vinculó un DNI a la identidad de la cuenta.", cancellationToken);
            return;
        }
        await _repositorioUsuarios.ConsolidarIdentidadesAsync(origen.Id, destino, cancellationToken);
        await RegistrarAuditoriaAsync(actorUsuarioId, usuarioId, null, "Identidades consolidadas", "Root vinculó la cuenta a una identidad existente por DNI.", cancellationToken);
    }

    public async Task RegistrarAuditoriaAsync(Guid actorUsuarioId, Guid? usuarioAfectadoId, Guid? institucionId, string accion, string detalle, CancellationToken cancellationToken = default)
        => await _repositorioUsuarios.RegistrarAuditoriaAsync(new EventoAuditoriaPersonal(actorUsuarioId, usuarioAfectadoId, institucionId, accion, detalle), cancellationToken);

    public async Task<IReadOnlyList<DatosAuditoriaPersonal>> ListarAuditoriaAsync(CancellationToken cancellationToken = default)
        => (await _repositorioUsuarios.ListarAuditoriaAsync(cancellationToken))
            .Select(e => new DatosAuditoriaPersonal(e.Id, e.ActorUsuarioId, e.UsuarioAfectadoId, e.InstitucionId, e.Accion, e.Detalle, e.FechaUtc)).ToList();

    public async Task<bool> CambiarEstadoUsuarioAsync(
        Guid usuarioId,
        bool activo,
        Guid actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return false;

        var esRoot = (await _servicioIdentidad.ObtenerRolesAsync(usuarioId, cancellationToken))
            .Contains(RolesPersonal.AdministradorDelSistema);
        if (activo)
        {
            usuario.Activar();
            await _repositorioUsuarios.ActualizarConAuditoriaAsync(usuario,
                [new EventoAuditoriaPersonal(actorUsuarioId, usuarioId, null, "Cuenta habilitada", "La cuenta fue habilitada. Los accesos institucionales revocados no se restauran automáticamente.")], cancellationToken);
            return true;
        }

        if (actorUsuarioId == usuarioId)
            throw new InvalidOperationException("No podés deshabilitar tu propia cuenta Root.");
        if (esRoot)
        {
            var cuentas = await _repositorioUsuarios.ListarTodosAsync(cancellationToken);
            var rootsActivos = 0;
            foreach (var cuenta in cuentas.Where(c => c.Activo))
            {
                if ((await _servicioIdentidad.ObtenerRolesAsync(cuenta.Id, cancellationToken))
                    .Contains(RolesPersonal.AdministradorDelSistema)) rootsActivos++;
            }
            if (rootsActivos <= 1)
                throw new InvalidOperationException("No se puede deshabilitar la última cuenta Root activa.");
        }

        var auditoria = new List<EventoAuditoriaPersonal>();
        foreach (var acceso in usuario.Accesos.ToList())
        {
            auditoria.Add(new EventoAuditoriaPersonal(actorUsuarioId, usuarioId, acceso.InstitucionId,
                "Acceso revocado por baja global", $"Se revocó el rol {acceso.Rol} como parte de la deshabilitación global."));
            usuario.RevocarAcceso(acceso.InstitucionId);
        }
        usuario.Desactivar();
        auditoria.Add(new EventoAuditoriaPersonal(actorUsuarioId, usuarioId, null, "Cuenta deshabilitada",
            esRoot ? "Root deshabilitó la cuenta global. No se quitaron accesos institucionales." : "Root revocó todos los accesos institucionales y deshabilitó la cuenta."));
        await _repositorioUsuarios.ActualizarConAuditoriaAsync(usuario, auditoria, cancellationToken);
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

        if ((await _servicioIdentidad.ObtenerRolesAsync(usuarioId, cancellationToken))
            .Contains(RolesPersonal.AdministradorDelSistema))
            throw new InvalidOperationException("Una cuenta Root no puede recibir accesos institucionales.");

        usuario.AsignarAcceso(peticion.InstitucionId, peticion.Rol);
        await _repositorioUsuarios.ActualizarAsync(usuario, cancellationToken);
        return true;
    }

    public async Task<bool> RevocarAccesoInstitucionAsync(
        Guid usuarioId,
        Guid institucionId,
        Guid actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _repositorioUsuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return false;

        var resultado = usuario.RevocarAcceso(institucionId);
        if (!resultado)
            return false;

        await _repositorioUsuarios.ActualizarAsync(usuario, cancellationToken);
        await RegistrarAuditoriaAsync(actorUsuarioId, usuarioId, institucionId, "Acceso revocado", "Se revocó el acceso de la cuenta a esta institución.", cancellationToken);
        return true;
    }

    private static DatosUsuarioPersonal MapearADatosUsuarioPersonal(
        UsuarioPersonal usuario,
        IReadOnlyList<string> rolesGlobales)
    {
        var accesosDto = usuario.Accesos
            .Select(a => new DatosAccesoInstitucion(a.Id, a.InstitucionId, a.Rol))
            .ToList();

        return new DatosUsuarioPersonal(
            usuario.Id,
            usuario.Nombre,
            usuario.CorreoElectronico,
            usuario.Activo,
            rolesGlobales,
            accesosDto,
            usuario.IdentidadPersonalId,
            usuario.IdentidadPersonal?.DniNormalizado is { Length: > 3 } dni ? $"••••{dni[^4..]}" : null);
    }
}
