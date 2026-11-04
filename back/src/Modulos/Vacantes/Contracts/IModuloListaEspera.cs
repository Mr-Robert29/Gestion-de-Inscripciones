namespace Jardin.Vacantes.Contracts;

public interface IModuloListaEspera
{
    Task<ConsultarListaEsperaRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid periodoId,
        ConsultarListaEsperaPeticion peticion,
        CancellationToken tokenCancelacion = default);
}
