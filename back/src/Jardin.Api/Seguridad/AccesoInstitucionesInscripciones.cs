using System.Security.Claims;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;

namespace Jardin.Api.Seguridad;

// Integra módulos por sus contratos; Inscripciones no consulta tablas de Usuarios ni Configuración.
public sealed class AccesoInstitucionesInscripciones : IAccesoInstitucionesInscripciones
{
    private readonly IHttpContextAccessor _http;
    private readonly IModuloUsuarios _usuarios;
    private readonly IModuloConfiguracion _configuracion;

    public AccesoInstitucionesInscripciones(
        IHttpContextAccessor http,
        IModuloUsuarios usuarios,
        IModuloConfiguracion configuracion)
    {
        _http = http;
        _usuarios = usuarios;
        _configuracion = configuracion;
    }

    // Implementa IAccesoInstitucionesInscripciones.AutorizarAsync: obtiene permisos actuales, no roles viejos del JWT.
    public Task<Guid> AutorizarAsync(
        Guid institucionId,
        bool escritura,
        CancellationToken tokenCancelacion = default)
        => AutorizarOperacionAsync(
            institucionId,
            escritura ? OperacionInscripciones.RegistrarPersonasOSolicitudes : OperacionInscripciones.Consultar,
            tokenCancelacion);

    // Implementa IAccesoInstitucionesInscripciones.AutorizarOperacionAsync: valida el permiso concreto.
    public async Task<Guid> AutorizarOperacionAsync(
        Guid institucionId,
        OperacionInscripciones operacion,
        CancellationToken tokenCancelacion = default)
    {
        tokenCancelacion.ThrowIfCancellationRequested();
        var principal = _http.HttpContext?.User;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal?.FindFirstValue("sub");
        if (principal?.Identity?.IsAuthenticated != true || !Guid.TryParse(id, out var usuarioId))
            throw new UnauthorizedAccessException();
        var sesion = await _usuarios.ObtenerSesionActualAsync(usuarioId, tokenCancelacion);
        if (sesion is null || !sesion.Activo) throw new UnauthorizedAccessException();
        var permiso = operacion switch
        {
            OperacionInscripciones.Consultar => PermisosPersonal.ConsultarSolicitudes,
            OperacionInscripciones.RegistrarPersonasOSolicitudes => PermisosPersonal.RegistrarSolicitudes,
            OperacionInscripciones.RegistrarDocumentacion => PermisosPersonal.RegistrarDocumentacion,
            OperacionInscripciones.VerificarCondiciones => PermisosPersonal.VerificarCondicionesPrioridad,
            OperacionInscripciones.DetectarDuplicados => PermisosPersonal.RegistrarSolicitudes,
            OperacionInscripciones.ResolverDuplicados => PermisosPersonal.ResolverDuplicaciones,
            OperacionInscripciones.RegistrarDistanciaManual => PermisosPersonal.RevisarDomicilios,
            OperacionInscripciones.CorregirDuranteRecepcion => PermisosPersonal.CorregirDuranteRecepcion,
            OperacionInscripciones.CorregirDespuesDelCierre => PermisosPersonal.CorregirDespuesDelCierre,
            _ => throw new ArgumentOutOfRangeException(nameof(operacion))
        };
        var esRoot = sesion.Roles.Contains(RolesPersonal.AdministradorDelSistema);
        if (!esRoot && !sesion.Accesos.Any(a => a.InstitucionId == institucionId &&
            PermisosPersonal.ObtenerPermisosPorRol(a.Rol).Contains(permiso)))
            throw new UnauthorizedAccessException("No tenés permiso para esta institución.");
        if (await _configuracion.ConsultarInstitucionAsync(institucionId, tokenCancelacion) is null)
            throw new KeyNotFoundException("No se encontró la institución.");
        return sesion.UsuarioId;
    }
}
