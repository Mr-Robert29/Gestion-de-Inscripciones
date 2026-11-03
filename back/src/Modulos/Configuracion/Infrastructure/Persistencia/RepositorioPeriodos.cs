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
}
