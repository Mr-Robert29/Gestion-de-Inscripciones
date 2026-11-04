namespace Jardin.Vacantes.Contracts;

public sealed record ConsultarListaEsperaRespuesta(
    Guid InstitucionId,
    Guid PeriodoId,
    DateTimeOffset FechaConsulta,
    Guid? PropuestaConfirmadaId,
    Guid? PropuestaOrigenOrdenId,
    DateTimeOffset? FechaConfirmacion,
    Guid? VersionReglasId,
    Guid? VersionOfertaId,
    string EstadoProceso,
    bool RequiereRevision,
    IReadOnlyList<string> Avisos,
    IReadOnlyList<string> OrdenProcedimiento,
    int TotalEnEspera,
    int TotalPendientes,
    DatosPaginaListaEspera EnEspera,
    DatosPaginaListaEspera Pendientes);

public sealed record DatosPaginaListaEspera(
    IReadOnlyList<DatosIntegranteListaEspera> Resultados,
    int TotalFiltrado,
    int Pagina,
    int TamanoPagina,
    bool HayMas);

public sealed record DatosIntegranteListaEspera(
    Guid SolicitudId,
    Guid AspiranteId,
    string Nombre,
    string Apellido,
    string? DniUtilizado,
    int? Sala,
    int EdadUtilizada,
    int? PosicionGeneral,
    int? PosicionEspera,
    string Estado,
    string Motivo,
    Guid TurnoPreferidoId,
    IReadOnlyList<Guid> TurnosAlternativosAceptados,
    IReadOnlyList<string> CondicionesVerificadas,
    decimal? DistanciaMetrosUtilizada,
    decimal? DistanciaComparada,
    Guid? MedicionUtilizadaId,
    bool DatosCoincidenConResultado,
    DatosSorteoListaEspera? Sorteo,
    IReadOnlyList<DatosResolucionListaEspera> CasosRevision);

public sealed record DatosSorteoListaEspera(
    Guid SorteoId,
    int Version,
    string Numero,
    int? TerminacionDni,
    int? ClaveCircular,
    bool GrupoConIgualPrioridad);

public sealed record DatosResolucionListaEspera(
    Guid CasoId,
    string Tipo,
    string Estado,
    int? PosicionDentroDelCaso,
    string? Motivo);
