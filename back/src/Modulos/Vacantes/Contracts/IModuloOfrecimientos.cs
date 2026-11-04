namespace Jardin.Vacantes.Contracts;

public interface IModuloOfrecimientos
{
    Task<ConsultarCandidatosVacanteRespuesta> ConsultarCandidatosAsync(
        Guid periodoId,
        Guid ofertaId,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarOfrecimientoRespuesta> OfrecerAsync(
        Guid periodoId,
        OfrecerVacantePeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarOfrecimientoRespuesta?> ConsultarAsync(Guid ofrecimientoId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarOfrecimientoRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<ConsultarOfrecimientoRespuesta> ResponderAsync(
        Guid ofrecimientoId,
        ResponderOfrecimientoPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarOfrecimientoRespuesta> ConfirmarAsync(
        Guid ofrecimientoId,
        ConfirmarOfrecimientoPeticion peticion,
        CancellationToken tokenCancelacion = default);
}

public sealed record OfrecerVacantePeticion(
    Guid OperacionId,
    Guid SolicitudId,
    Guid OfertaId,
    Guid PropuestaListaId,
    string Motivo);
public sealed record ResponderOfrecimientoPeticion(
    int Revision,
    string Respuesta,
    string Motivo,
    string? Referencia = null);
public sealed record ConfirmarOfrecimientoPeticion(int Revision, string Motivo);
public sealed record ConsultarCandidatosVacanteRespuesta(
    Guid PeriodoId,
    Guid OfertaId,
    Guid? PropuestaListaId,
    IReadOnlyList<DatosIntegranteListaEspera> Candidatos);
public sealed record ConsultarOfrecimientoRespuesta(
    Guid Id,
    Guid OperacionId,
    Guid PeriodoId,
    Guid InstitucionId,
    Guid SolicitudId,
    Guid AspiranteId,
    Guid OfertaId,
    Guid TurnoId,
    Guid PropuestaListaId,
    Guid ReservaId,
    string Estado,
    int Revision,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string Motivo,
    string? ReferenciaRespuesta,
    Guid? IngresoId,
    IReadOnlyList<DatosCambioOfrecimiento> Historial);
public sealed record DatosCambioOfrecimiento(
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string EstadoAnterior,
    string EstadoNuevo,
    string Motivo,
    string? Referencia = null);
