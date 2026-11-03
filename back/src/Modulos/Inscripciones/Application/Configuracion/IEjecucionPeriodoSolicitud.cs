namespace Jardin.Inscripciones.Application.Configuracion;

// La integración con Configuración ejecuta el registro mientras protege el estado del período.
public interface IEjecucionPeriodoSolicitud
{
    Task<T> EjecutarAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Func<DatosPeriodoSolicitud, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
    // Para revisar antecedentes no necesita interpretar versiones actuales de reglas/oferta.
    Task<T> EjecutarRevisionAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Func<string, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
    Task<T> EjecutarCorreccionAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Guid versionOfertaId,
        Func<DatosPeriodoCorreccionSolicitud, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}

public sealed record DatosPeriodoSolicitud(
    Guid InstitucionId,
    Guid PeriodoId,
    string Estado,
    int Revision,
    Guid VersionReglasId,
    Guid VersionOfertaId,
    DateOnly FechaCorte,
    IReadOnlyList<Guid> TurnosDisponibles);

public sealed record DatosPeriodoCorreccionSolicitud(string Estado, IReadOnlyList<Guid> TurnosDisponibles);
