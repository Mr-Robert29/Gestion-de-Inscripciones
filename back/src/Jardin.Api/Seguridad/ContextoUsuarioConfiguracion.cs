using System.Security.Claims;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;

namespace Jardin.Api.Seguridad;

// Traduce la sesión actual a los permisos que necesita Configuración, sin confiar en roles viejos del JWT.
public sealed class ContextoUsuarioConfiguracion : IContextoUsuarioConfiguracion
{
    private readonly IHttpContextAccessor _http;
    private readonly IModuloUsuarios _usuarios;

    public ContextoUsuarioConfiguracion(IHttpContextAccessor http, IModuloUsuarios usuarios)
    {
        _http = http;
        _usuarios = usuarios;
    }

    // Implementa IContextoUsuarioConfiguracion.ObtenerAsync.
    public async Task<UsuarioConfiguracion?> ObtenerAsync(CancellationToken tokenCancelacion = default)
    {
        var principal = _http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;
        var identificador = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(identificador, out var usuarioId)) return null;
        var sesion = await _usuarios.ObtenerSesionActualAsync(usuarioId, tokenCancelacion);
        if (sesion is null || !sesion.Activo) return null;
        var esRoot = sesion.Roles.Contains(RolesPersonal.AdministradorDelSistema);
        var puedeRegistrar = sesion.Permisos.Contains(PermisosPersonal.ConfigurarInstitucionYOferta);
        var accesos = sesion.Accesos
            .Where(a => RolesPersonal.Todos.Contains(a.Rol))
            .ToDictionary(a => a.InstitucionId, a => PermisosPersonal.ObtenerPermisosPorRol(a.Rol).Contains(PermisosPersonal.ConfigurarInstitucionYOferta));
        return new UsuarioConfiguracion(
            sesion.UsuarioId,
            esRoot,
            puedeRegistrar,
            accesos);
    }
}
