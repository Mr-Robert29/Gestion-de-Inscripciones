using Jardin.Vacantes.Application.Propuestas;

namespace Jardin.Vacantes.Application.Servicios;

// La exclusión local cubre también SQLite; PostgreSQL añade el bloqueo del período entre instancias.
public sealed class EjecucionEspera
{
    private static readonly SemaphoreSlim PuertaLocal = new(1, 1);
    private readonly IContextoPropuestaIngresos _contexto;
    public EjecucionEspera(IContextoPropuestaIngresos contexto) { _contexto = contexto; }
    public async Task<T> EjecutarAsync<T>(
        Guid periodoId,
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion)
    {
        await PuertaLocal.WaitAsync(tokenCancelacion);
        try { return await _contexto.ConPeriodoProtegidoAsync(periodoId, operacion, tokenCancelacion); }
        finally { PuertaLocal.Release(); }
    }
}
