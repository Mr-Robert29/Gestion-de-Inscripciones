using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

public sealed class RepositorioOferta : IRepositorioOferta
{
    private readonly ContextoConfiguracion _contexto;
    public RepositorioOferta(ContextoConfiguracion contexto) { _contexto = contexto; }

    // Implementa IRepositorioOferta.ObtenerAulaAsync.
    public Task<Aula?> ObtenerAulaAsync(Guid id, CancellationToken tokenCancelacion = default)
        => _contexto.Aulas.SingleOrDefaultAsync(a => a.Id == id, tokenCancelacion);

    // Implementa IRepositorioOferta.ObtenerTurnoAsync.
    public Task<Turno?> ObtenerTurnoAsync(Guid id, CancellationToken tokenCancelacion = default)
        => _contexto.Turnos.SingleOrDefaultAsync(t => t.Id == id, tokenCancelacion);

    // Implementa IRepositorioOferta.ListarAulasAsync.
    public async Task<IReadOnlyList<Aula>> ListarAulasAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
        => await _contexto.Aulas.AsNoTracking().Where(a => a.InstitucionId == institucionId).OrderBy(a => a.Nombre).ToArrayAsync(tokenCancelacion);

    // Implementa IRepositorioOferta.ListarTurnosAsync.
    public async Task<IReadOnlyList<Turno>> ListarTurnosAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
        => await _contexto.Turnos.AsNoTracking().Where(t => t.InstitucionId == institucionId).OrderBy(t => t.Nombre).ToArrayAsync(tokenCancelacion);

    // Implementa IRepositorioOferta.ObtenerOfertaAsync.
    public Task<OfertaAulaTurno?> ObtenerOfertaAsync(Guid id, CancellationToken tokenCancelacion = default)
        => _contexto.Ofertas.Include(o => o.Ocupaciones).SingleOrDefaultAsync(o => o.Id == id, tokenCancelacion);

    // Implementa IRepositorioOferta.ListarOfertasAsync.
    public async Task<IReadOnlyList<OfertaAulaTurno>> ListarOfertasAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => await _contexto.Ofertas.AsNoTracking().Include(o => o.Ocupaciones).Where(o => o.PeriodoId == periodoId).OrderBy(o => o.Id).ToArrayAsync(tokenCancelacion);

    // Implementa IRepositorioOferta.ExisteCombinacionAsync.
    public Task<bool> ExisteCombinacionAsync(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        CancellationToken tokenCancelacion = default)
        => _contexto.Ofertas.AnyAsync(o => o.PeriodoId == periodoId && o.AulaId == aulaId && o.TurnoId == turnoId, tokenCancelacion);

    // Implementa IRepositorioOferta.AgregarAulaAsync.
    public Task AgregarAulaAsync(
        Aula aula,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Aulas.Add(aula);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    // Implementa IRepositorioOferta.AgregarTurnoAsync.
    public Task AgregarTurnoAsync(
        Turno turno,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Turnos.Add(turno);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    // Implementa IRepositorioOferta.AgregarOfertaAsync.
    public async Task AgregarOfertaAsync(
        OfertaAulaTurno oferta,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Ofertas.Add(oferta);
        try { await GuardarAsync(cambio, tokenCancelacion); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_Oferta_Periodo_Aula_Turno" })
        {
            // Protege también dos altas simultáneas que pasaron la comprobación previa.
            throw new InvalidOperationException("Ya existe una oferta para ese período, aula y turno.", error);
        }
    }

    // Implementa IRepositorioOferta.ActualizarOfertaAsync.
    public Task ActualizarOfertaAsync(
        OfertaAulaTurno oferta,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        if (_contexto.Entry(oferta).State == EntityState.Detached)
            throw new InvalidOperationException("Consultá la oferta antes de modificarla.");
        return GuardarAsync(cambio, tokenCancelacion);
    }

    private async Task GuardarAsync(CambioInstitucion cambio, CancellationToken tokenCancelacion)
    {
        _contexto.Cambios.Add(cambio);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoInstitucionException(); }
    }
}
