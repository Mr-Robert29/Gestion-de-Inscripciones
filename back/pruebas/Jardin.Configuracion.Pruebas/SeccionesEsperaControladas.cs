using Jardin.Secciones.Contracts;

namespace Jardin.Configuracion.Pruebas;

// Permite comprobar fallos de coordinación sin tocar bases ni servicios reales.
internal sealed class SeccionesEsperaControladas : IModuloSeccionesEspera
{
    private readonly IModuloSeccionesEspera _real;
    private readonly Func<bool> _fallar;
    public SeccionesEsperaControladas(IModuloSeccionesEspera real, Func<bool> fallar) { _real = real; _fallar = fallar; }

    // Implementa IModuloSeccionesEspera.DarBajaAsignacionesAsync con un fallo controlado para la prueba.
    public Task DarBajaAsignacionesAsync(
        Guid periodoId,
        Guid ingresoId,
        Guid usuarioId,
        string motivo,
        CancellationToken tokenCancelacion = default)
    {
        if (_fallar()) throw new InvalidOperationException("Fallo de Secciones simulado.");
        return _real.DarBajaAsignacionesAsync(periodoId, ingresoId, usuarioId, motivo, tokenCancelacion);
    }

    // Implementa IModuloSeccionesEspera.ConOfertaProtegidaAsync delegando la protección real.
    public Task<T> ConOfertaProtegidaAsync<T>(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        Func<IReadOnlyList<DatosSeccion>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => _real.ConOfertaProtegidaAsync(periodoId, aulaId, turnoId, operacion, tokenCancelacion);
}
