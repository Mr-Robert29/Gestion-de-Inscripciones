namespace Jardin.Vacantes.Contracts;

public sealed record DatosEvaluacionPrioridad(
    Guid Id,
    Guid PeriodoId,
    Guid VersionReglasId,
    Guid UsuarioId,
    int RevisionPeriodo,
    DateTimeOffset Fecha,
    IReadOnlyList<DatosPrioridad> Resultados);
