using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

public sealed class RepositorioPeriodos : IRepositorioPeriodos
{
    private readonly ContextoConfiguracion _contexto;

    public RepositorioPeriodos(ContextoConfiguracion contexto) { _contexto = contexto; }

    // Implementa IRepositorioPeriodos.ObtenerAsync.
    public Task<PeriodoInscripcion?> ObtenerAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        return _contexto.Periodos.SingleOrDefaultAsync(p => p.Id == periodoId, tokenCancelacion);
    }

    // Implementa IRepositorioPeriodos.ListarAsync.
    public async Task<IReadOnlyList<PeriodoInscripcion>> ListarAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        return await _contexto.Periodos.AsNoTracking().Where(p => p.InstitucionId == institucionId)
            .OrderByDescending(p => p.Ciclo).ThenBy(p => p.Inicio).ToArrayAsync(tokenCancelacion);
    }

    // Implementa IRepositorioPeriodos.AgregarAsync.
    public async Task AgregarAsync(
        PeriodoInscripcion periodo,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Periodos.Add(periodo);
        _contexto.Cambios.Add(cambio);
        await _contexto.SaveChangesAsync(tokenCancelacion);
    }

    // Implementa IRepositorioPeriodos.ActualizarAsync.
    public async Task ActualizarAsync(
        PeriodoInscripcion periodo,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        if (_contexto.Entry(periodo).State == EntityState.Detached)
            throw new InvalidOperationException("Consultá el período antes de modificarlo.");
        _contexto.Cambios.Add(cambio);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoInstitucionException(); }
    }
    // Implementa IRepositorioPeriodos.EjecutarConBloqueoAsync: FOR UPDATE coordina también distintas instancias de la API.
    public async Task<T> EjecutarConBloqueoAsync<T>(
        Guid periodoId,
        Func<PeriodoInscripcion, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(tokenCancelacion);
        var consulta = _contexto.Database.IsNpgsql()
            ? _contexto.Periodos.FromSqlInterpolated($"SELECT * FROM configuracion.periodos WHERE \"Id\" = {periodoId} FOR UPDATE")
            : _contexto.Periodos.AsQueryable();
        // La lectura protegida es nueva: no reutiliza una entidad previamente seguida por el contexto.
        var periodo = await consulta.AsNoTracking().SingleOrDefaultAsync(p => p.Id == periodoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el período.");
        var resultado = await operacion(periodo, tokenCancelacion);
        await transaccion.CommitAsync(tokenCancelacion);
        return resultado;
    }
}
