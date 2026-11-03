namespace Jardin.Configuracion.Contracts;

public interface IModuloVersionesConfiguracion
{
    Task<PublicarVersionRespuesta> PublicarReglasAsync(
        Guid periodoId,
        PublicarReglasPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<PublicarVersionRespuesta> PublicarOfertaAsync(
        Guid periodoId,
        PublicarOfertaPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<ConsultarVersionRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<ConsultarVersionRespuesta?> ConsultarAsync(Guid versionId, CancellationToken tokenCancelacion = default);
    Task<AsignarConfiguracionRespuesta> AsignarAsync(
        Guid periodoId,
        AsignarConfiguracionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    // Comprobación informativa; la confirmación real debe coordinarse transaccionalmente con su módulo.
    Task<bool> EstaVigenteAsync(
        Guid periodoId,
        DatosVersionesConfiguracion versiones,
        CancellationToken tokenCancelacion = default);
}
