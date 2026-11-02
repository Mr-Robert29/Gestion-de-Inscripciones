namespace Jardin.Configuracion.Application.Seguridad;

// El host obtiene esta identidad de la sesión autenticada; nunca de la petición del cliente.
public interface IContextoUsuarioConfiguracion
{
    Task<UsuarioConfiguracion?> ObtenerAsync(CancellationToken tokenCancelacion = default);
}

public sealed record UsuarioConfiguracion(
    Guid Id,
    bool EsRoot,
    bool PuedeRegistrar,
    IReadOnlyDictionary<Guid, bool> Accesos);
