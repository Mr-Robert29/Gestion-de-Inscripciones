namespace Jardin.Vacantes.Contracts;

public sealed record DatosEstadoProcesamientoPrioridades(
    bool RecepcionCerrada,
    bool Procesado,
    bool Vigente,
    DateTimeOffset? FechaProcesamiento,
    int CantidadSolicitudes,
    int PendientesRevision,
    IReadOnlyList<DatosPrioridad> Resultados);
