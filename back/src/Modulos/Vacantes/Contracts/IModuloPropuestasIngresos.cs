namespace Jardin.Vacantes.Contracts;

public interface IModuloPropuestasIngresos
{
    Task<ConsultarPropuestaIngresosRespuesta> GenerarAsync(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarPropuestaIngresosRespuesta?> ConsultarAsync(Guid propuestaId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarPropuestaIngresosRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
