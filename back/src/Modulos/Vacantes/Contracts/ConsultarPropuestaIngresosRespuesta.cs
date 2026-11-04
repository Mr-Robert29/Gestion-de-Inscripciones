namespace Jardin.Vacantes.Contracts;

// Vigencia informativa: HU-19 debe volver a proteger las entradas al confirmar.
public sealed record ConsultarPropuestaIngresosRespuesta(
    Guid Id,
    Guid InstitucionId,
    Guid PeriodoId,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string Motivo,
    Guid VersionReglasId,
    Guid VersionOfertaId,
    string Estado,
    int Revision,
    Guid? PropuestaAnteriorId,
    bool EntradasVigentes,
    string HuellaEntradas,
    IReadOnlyList<DatosResultadoPropuesta> Resultados);

public sealed record DatosResultadoPropuesta(
    Guid SolicitudId,
    Guid AspiranteId,
    int? Sala,
    string Estado,
    Guid? OfertaId,
    Guid? TurnoId,
    bool EsTurnoAlternativo,
    string Explicacion);
