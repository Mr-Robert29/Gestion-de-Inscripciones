using System.Security.Claims;
using Jardin.Configuracion.Contracts;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Vacantes.Application.Seguridad;

namespace Jardin.Api.Seguridad;

public sealed class AccesoVacantes :
    IAutorizacionVacantes,
    IAutorizacionPropuestasIngresos,
    IAutorizacionListaEspera,
    IAutorizacionOfrecimientos,
    IAutorizacionNominaDistrito
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

    // Implementa IAutorizacionPropuestasIngresos.AutorizarPropuestaAsync con el permiso específico de HU-18.
    public Task<AutorizacionVacantes> AutorizarPropuestaAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => AutorizarConPermisoAsync(
            periodoId,
            PermisosPersonal.GenerarPropuestasIngreso,
            "No tenés permiso para generar o consultar propuestas en esta institución.",
            tokenCancelacion);

    public Task<AutorizacionVacantes> AutorizarModificacionAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => AutorizarConPermisoAsync(periodoId, PermisosPersonal.ModificarPropuestasIngreso, "No tenés permiso para modificar propuestas en esta institución.", tokenCancelacion);

    public Task<AutorizacionVacantes> AutorizarAceptacionTurnoAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => AutorizarConPermisoAsync(periodoId, PermisosPersonal.RegistrarRespuestaOfrecimiento, "No tenés permiso para registrar aceptaciones en esta institución.", tokenCancelacion);

    public Task<AutorizacionVacantes> AutorizarConfirmacionAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => AutorizarConPermisoAsync(periodoId, PermisosPersonal.ConfirmarIngresos, "No tenés permiso para confirmar ingresos en esta institución.", tokenCancelacion);

    // Implementa IAutorizacionListaEspera.AutorizarConsultaEsperaAsync: consulta sin permiso de confirmación.
    public Task<AutorizacionVacantes> AutorizarConsultaEsperaAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => AutorizarConPermisoAsync(
            periodoId,
            PermisosPersonal.ConsultarListaEspera,
            "No tenés permiso para consultar la lista de espera de esta institución.",
            tokenCancelacion);

public Task<AutorizacionVacantes> AutorizarOfertaAsync(
    Guid periodoId,
    CancellationToken tokenCancelacion = default)
    => AutorizarConPermisoAsync(
        periodoId,
        PermisosPersonal.RegistrarOfrecimientos,
        "No tenés permiso para ofrecer vacantes.",
        tokenCancelacion);

// Implementa IAutorizacionOfrecimientos.AutorizarRespuestaAsync.
public Task<AutorizacionVacantes> AutorizarRespuestaAsync(
    Guid periodoId,
    CancellationToken tokenCancelacion = default)
    => AutorizarConPermisoAsync(
        periodoId,
        PermisosPersonal.RegistrarRespuestaOfrecimiento,
        "No tenés permiso para registrar respuestas.",
        tokenCancelacion);

// Implementa IAutorizacionOfrecimientos.AutorizarConfirmacionEsperaAsync.
public Task<AutorizacionVacantes> AutorizarConfirmacionEsperaAsync(
    Guid periodoId,
    CancellationToken tokenCancelacion = default)
    => AutorizarConPermisoAsync(
        periodoId,
        PermisosPersonal.ConfirmarIngresoDesdeEspera,
        "No tenés permiso para confirmar ingresos desde espera.",
        tokenCancelacion);

// Implementa IAutorizacionOfrecimientos.AutorizarBajaAsync.
public Task<AutorizacionVacantes> AutorizarBajaAsync(
    Guid periodoId,
    CancellationToken tokenCancelacion = default)
    => AutorizarConPermisoAsync(
        periodoId,
        PermisosPersonal.RegistrarBajaDefinitiva,
        "No tenés permiso para registrar bajas.",
        tokenCancelacion);

// Implementa IAutorizacionNominaDistrito.AutorizarConsultaAsync.
public Task<AutorizacionVacantes> AutorizarConsultaAsync(
    Guid periodoId,
    CancellationToken tokenCancelacion = default)
    => AutorizarConPermisoAsync(
        periodoId,
        PermisosPersonal.ConsultarReportes,
        "No tenés permiso para consultar la nómina distrital de esta institución.",
        tokenCancelacion);
}
