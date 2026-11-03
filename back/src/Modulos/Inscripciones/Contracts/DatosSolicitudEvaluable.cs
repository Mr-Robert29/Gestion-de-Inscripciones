namespace Jardin.Inscripciones.Contracts;

public sealed record DatosSolicitudEvaluable(
    Guid SolicitudId,
    Guid AspiranteId,
    Guid InstitucionId,
    Guid PeriodoId,
    int? Sala,
    bool FueraDeTermino,
    IReadOnlyList<string> CondicionesVerificadas,
    decimal? DistanciaMetros,
    int RevisionDatos,
    int RevisionDomicilio);
