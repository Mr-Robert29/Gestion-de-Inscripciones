using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class RepositorioDistribuciones : IRepositorioDistribuciones
{
    private readonly ContextoSecciones _contexto;

    public RepositorioDistribuciones(ContextoSecciones contexto)
    {
        _contexto = contexto;
    }

    public Task<int> ObtenerUltimaVersionAsync(Guid periodoId, CancellationToken cancellationToken = default) =>
        _contexto.PropuestasDistribucion
            .Where(x => x.PeriodoId == periodoId)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken)
            .ContinueWith(x => x.Result ?? 0, cancellationToken);

    public async Task AgregarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default)
    {
        await _contexto.PropuestasDistribucion.AddAsync(propuesta, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public Task<PropuestaDistribucion?> ObtenerPorIdAsync(Guid propuestaId, CancellationToken cancellationToken = default) =>
        _contexto.PropuestasDistribucion
            .Include(x => x.Asignaciones)
            .FirstOrDefaultAsync(x => x.Id == propuestaId, cancellationToken);

    public async Task ActualizarAsync(PropuestaDistribucion propuesta, CancellationToken cancellationToken = default)
    {
        _contexto.PropuestasDistribucion.Update(propuesta);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExisteAsignacionActivaAsync(Guid ingresoId, CancellationToken cancellationToken = default) =>
        _contexto.AsignacionesSeccion.AnyAsync(
            x => x.IngresoId == ingresoId && x.Estado == Domain.EstadoAsignacionSeccion.Activa,
            cancellationToken);

    public async Task ConfirmarAsync(
        PropuestaDistribucion propuesta,
        IReadOnlyList<AsignacionSeccion> asignaciones,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        _contexto.PropuestasDistribucion.Update(propuesta);
        await _contexto.AsignacionesSeccion.AddRangeAsync(asignaciones, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }
}
