namespace Jardin.Inscripciones.Application.Seguridad;

// El host comprueba sesión, permisos y existencia de la institución; devuelve el autor autenticado.
public interface IAccesoInstitucionesInscripciones
{
    Task<Guid> AutorizarAsync(
        Guid institucionId,
        bool escritura,
        CancellationToken tokenCancelacion = default);
    Task<Guid> AutorizarOperacionAsync(
        Guid institucionId,
        OperacionInscripciones operacion,
        CancellationToken tokenCancelacion = default);
}
