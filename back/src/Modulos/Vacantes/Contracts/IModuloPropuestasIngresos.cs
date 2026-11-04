namespace Jardin.Vacantes.Contracts;

public interface IModuloPropuestasIngresos
{
    Task<ConsultarPropuestaIngresosRespuesta> GenerarAsync(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarPropuestaIngresosRespuesta?> ConsultarAsync(Guid propuestaId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarPropuestaIngresosRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<ConsultarPropuestaIngresosRespuesta> ModificarAsync(
        Guid propuestaId,
        ModificarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<DatosAceptacionTurno> RegistrarAceptacionTurnoAsync(
        Guid propuestaId,
        RegistrarAceptacionTurnoPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<DatosConfirmacionIngreso>> ConfirmarAsync(
        Guid propuestaId,
        ConfirmarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
