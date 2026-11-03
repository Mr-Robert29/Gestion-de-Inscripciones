namespace Jardin.Configuracion.Contracts;

// Operaciones de recursos y oferta del módulo Configuración.
public interface IModuloOferta
{
    Task<RegistrarAulaRespuesta> RegistrarAulaAsync(
        Guid institucionId,
        RegistrarAulaPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarAulaRespuesta>> ListarAulasAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<RegistrarTurnoRespuesta> RegistrarTurnoAsync(
        Guid institucionId,
        RegistrarTurnoPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarTurnoRespuesta>> ListarTurnosAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<RegistrarOfertaRespuesta> RegistrarOfertaAsync(
        Guid periodoId,
        RegistrarOfertaPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<ConsultarOfertaRespuesta?> ConsultarOfertaAsync(Guid ofertaId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarOfertaRespuesta>> ListarOfertasAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<ActualizarOfertaRespuesta> ActualizarOfertaAsync(
        Guid ofertaId,
        ActualizarOfertaPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
