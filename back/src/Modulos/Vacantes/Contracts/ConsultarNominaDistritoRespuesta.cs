namespace Jardin.Vacantes.Contracts;

public sealed record ConsultarNominaDistritoRespuesta(
    Guid InstitucionId,
    Guid PeriodoId,
    DateTimeOffset FechaGeneracion,
    string EstadoProceso,
    bool RequiereRevision,
    Guid? PropuestaConfirmadaId,
    DateTimeOffset? FechaConfirmacion,
    int TotalSinVacante,
    int TotalPendientes,
    IReadOnlyList<DatosNominaDistrito> SinVacante,
    IReadOnlyList<DatosNominaDistrito> Pendientes);

public sealed record DatosNominaDistrito(
    Guid SolicitudId,
    Guid AspiranteId,
    string Nombre,
    string Apellido,
    string? Dni,
    int? Sala,
    int Edad,
    int? PosicionGeneral,
    int? PosicionEspera,
    string Estado,
    string Motivo,
    Guid TurnoPreferidoId,
    decimal? DistanciaMetros,
    bool DatosCoincidenConResultado,
    IReadOnlyList<DatosResolucionListaEspera> CasosRevision);
