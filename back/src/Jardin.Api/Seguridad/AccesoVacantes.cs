using System.Security.Claims;
using Jardin.Configuracion.Contracts;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Vacantes.Application.Seguridad;

namespace Jardin.Api.Seguridad;

public sealed class AccesoVacantes : IAutorizacionVacantes
{
    private readonly IHttpContextAccessor _http;
    private readonly IModuloUsuarios _usuarios;
    private readonly IModuloPeriodos _periodos;

    public AccesoVacantes(
        IHttpContextAccessor http,
        IModuloUsuarios usuarios,
        IModuloPeriodos periodos)
    {
        _http = http;
        _usuarios = usuarios;
        _periodos = periodos;
    }

    public async Task<AutorizacionVacantes> AutorizarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var periodo = await _periodos.ConsultarPeriodoAsync(periodoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el período.");

        var principal = _http.HttpContext?.User;
        var identificador = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal?.FindFirstValue("sub");
        if (principal?.Identity?.IsAuthenticated != true || !Guid.TryParse(identificador, out var usuarioId))
            throw new UnauthorizedAccessException();

        var sesion = await _usuarios.ObtenerSesionActualAsync(usuarioId, cancellationToken);
        if (sesion is null || !sesion.Activo)
            throw new UnauthorizedAccessException();

        var esRoot = sesion.Roles.Contains(RolesPersonal.AdministradorDelSistema);
        var tieneAcceso = esRoot || sesion.Accesos.Any(acceso =>
            acceso.InstitucionId == periodo.InstitucionId &&
            PermisosPersonal.ObtenerPermisosPorRol(acceso.Rol)
                .Contains(PermisosPersonal.RegistrarSorteo));

        if (!tieneAcceso)
            throw new UnauthorizedAccessException("No tenés permiso para registrar sorteos en esta institución.");

        return new AutorizacionVacantes(usuarioId, periodo.InstitucionId);
    }
}
