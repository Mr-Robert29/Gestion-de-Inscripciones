using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public sealed record SolicitudEvaluableInterna(
    Guid SolicitudId,
    Guid AspiranteId,
    Guid InstitucionId,
    Guid PeriodoId,
    int? Sala,
    bool FueraDeTermino,
    IReadOnlyList<CondicionPrioridad> Condiciones,
    decimal? DistanciaMetros,
    int RevisionDatos,
    int RevisionDomicilio);
