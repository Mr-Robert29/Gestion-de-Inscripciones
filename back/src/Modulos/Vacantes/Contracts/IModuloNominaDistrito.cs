namespace Jardin.Vacantes.Contracts;

public interface IModuloNominaDistrito
{
    Task<ConsultarNominaDistritoRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default);
}
