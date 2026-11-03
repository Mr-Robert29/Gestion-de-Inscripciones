namespace Jardin.Inscripciones.Contracts;

public interface IModuloSolicitudesEvaluables
{
    Task<IReadOnlyList<DatosSolicitudEvaluable>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default);
}
