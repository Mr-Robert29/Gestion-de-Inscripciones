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
        => await AutorizarConPermisoAsync(
            periodoId,
            PermisosPersonal.RegistrarSorteo,
            "No tenés permiso para registrar sorteos en esta institución.",
            cancellationToken);

    public async Task<AutorizacionVacantes> AutorizarResolucionAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
        => await AutorizarConPermisoAsync(
            periodoId,
            PermisosPersonal.ResolverCasosEspeciales,
            "No tenés permiso para resolver casos especiales en esta institución.",
            cancellationToken);

    private async Task<AutorizacionVacantes> AutorizarConPermisoAsync(
        Guid periodoId,
        string permiso,
        string mensajeSinPermiso,
        CancellationToken cancellationToken)
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
                .Contains(permiso));

        if (!tieneAcceso)
            throw new UnauthorizedAccessException(mensajeSinPermiso);

        return new AutorizacionVacantes(usuarioId, periodo.InstitucionId);
    }
}
