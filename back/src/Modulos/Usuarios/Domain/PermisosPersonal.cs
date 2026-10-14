namespace Jardin.Usuarios.Domain;

public static class PermisosPersonal
{
    // Solicitudes
    public const string ConsultarSolicitudes = "Solicitudes.Consultar";
    public const string RegistrarSolicitudes = "Solicitudes.Registrar";
    public const string CorregirDuranteRecepcion = "Solicitudes.CorregirDuranteRecepcion";
    public const string CorregirDespuesDelCierre = "Solicitudes.CorregirDespuesDelCierre";
    public const string RegistrarDocumentacion = "Solicitudes.RegistrarDocumentacion";
    public const string VerificarCondicionesPrioridad = "Solicitudes.VerificarCondiciones";
    public const string RevisarDomicilios = "Solicitudes.RevisarDomicilios";
    public const string ResolverDuplicaciones = "Solicitudes.ResolverDuplicaciones";

    // Configuración y período
    public const string ConfigurarInstitucionYOferta = "Configuracion.InstitucionYOferta";
    public const string ConfigurarReglas = "Configuracion.Reglas";
    public const string AbrirYCerrarRecepcion = "Configuracion.AbrirYCerrarRecepcion";

    // Admisión, sorteo y vacantes
    public const string RegistrarSorteo = "Admision.RegistrarSorteo";
    public const string GenerarPropuestasIngreso = "Admision.GenerarPropuestas";
    public const string ModificarPropuestasIngreso = "Admision.ModificarPropuestas";
    public const string ResolverCasosEspeciales = "Admision.ResolverCasosEspeciales";
    public const string ConfirmarIngresos = "Admision.ConfirmarIngresos";
    public const string RegistrarBajaDefinitiva = "Admision.RegistrarBaja";
    public const string RegistrarOfrecimientos = "Admision.RegistrarOfrecimientos";
    public const string RegistrarRespuestaOfrecimiento = "Admision.RegistrarRespuestaOfrecimiento";
    public const string ConfirmarIngresoDesdeEspera = "Admision.ConfirmarIngresoDesdeEspera";

    // Secciones y distribución
    public const string DefinirSecciones = "Secciones.Definir";
    public const string GenerarDistribucion = "Secciones.GenerarDistribucion";
    public const string ModificarYConfirmarDistribucion = "Secciones.ConfirmarDistribucion";

    // Reportes y administración
    public const string ConsultarReportes = "Reportes.Consultar";
    public const string AdministrarUsuarios = "Usuarios.Administrar";

    private static readonly HashSet<string> PermisosAdministrativo = new()
    {
        ConsultarSolicitudes,
        RegistrarSolicitudes,
        CorregirDuranteRecepcion,
        RegistrarDocumentacion,
        VerificarCondicionesPrioridad,
        RevisarDomicilios,
        RegistrarSorteo,
        GenerarPropuestasIngreso,
        RegistrarOfrecimientos,
        RegistrarRespuestaOfrecimiento,
        GenerarDistribucion,
        ConsultarReportes
    };

    private static readonly HashSet<string> PermisosDirectivo = new(PermisosAdministrativo)
    {
        CorregirDespuesDelCierre,
        ResolverDuplicaciones,
        ConfigurarInstitucionYOferta,
        ConfigurarReglas,
        AbrirYCerrarRecepcion,
        ModificarPropuestasIngreso,
        ResolverCasosEspeciales,
        ConfirmarIngresos,
        RegistrarBajaDefinitiva,
        ConfirmarIngresoDesdeEspera,
        DefinirSecciones,
        ModificarYConfirmarDistribucion
    };

    public static IReadOnlySet<string> Todos { get; } = new HashSet<string>(PermisosDirectivo)
    {
        AdministrarUsuarios
    };

    public static IReadOnlySet<string> ObtenerPermisosPorRol(string rol)
    {
        return rol switch
        {
            RolesPersonal.Administrativo => PermisosAdministrativo,
            RolesPersonal.Directivo => PermisosDirectivo,
            RolesPersonal.AdministradorDelSistema => Todos,
            _ => new HashSet<string>()
        };
    }
}
