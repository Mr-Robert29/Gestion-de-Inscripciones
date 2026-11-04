namespace Jardin.Vacantes.Domain.Entidades;

public sealed record EntradasPropuestaIngresos(
    Guid InstitucionId,
    Guid PeriodoId,
    int RevisionPeriodo,
    string EstadoPeriodo,
    Guid VersionReglasId,
    Guid VersionOfertaId,
    IReadOnlyList<int> OrdenSalas,
    IReadOnlyList<string> OrdenProcedimiento,
    IReadOnlyList<int[]> Combinaciones,
    IReadOnlyList<OfertaPropuesta> Ofertas,
    IReadOnlyList<SolicitudPropuesta> Solicitudes,
    int PrecisionDistanciaDecimales = 2);

public sealed record OfertaPropuesta(
    Guid Id,
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    bool Disponible,
    IReadOnlyList<int> EdadesOcupacionesIniciales,
    int? CantidadOcupacionInicial = null);

public sealed record SolicitudPropuesta(
    Guid Id,
    Guid AspiranteId,
    int? Sala,
    bool FueraDeTermino,
    IReadOnlyList<string> Condiciones,
    decimal? DistanciaMetros,
    int RevisionDatos,
    int RevisionDomicilio,
    Guid TurnoPreferidoId,
    IReadOnlyList<Guid> Alternativas,
    string? Dni,
    Guid? MedicionId,
    DateTimeOffset UltimoCambio,
    bool DuplicacionPendiente,
    int Edad = 3);

