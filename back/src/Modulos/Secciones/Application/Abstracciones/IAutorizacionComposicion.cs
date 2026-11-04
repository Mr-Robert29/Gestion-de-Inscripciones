namespace Jardin.Secciones.Application.Abstracciones;

public interface IAutorizacionComposicion
{
    Task<Guid> AutorizarCambioAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
}
