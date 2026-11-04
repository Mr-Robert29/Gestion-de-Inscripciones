using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class RepositorioPropuestasIngresos : IRepositorioPropuestasIngresos
{
    private readonly ContextoVacantes _contexto;

    public RepositorioPropuestasIngresos(ContextoVacantes contexto) { _contexto = contexto; }

    // Implementa IRepositorioPropuestasIngresos.GuardarAsync: agrega una instantánea sin reemplazar anteriores.
    public async Task GuardarAsync(PropuestaIngresos propuesta, CancellationToken tokenCancelacion = default)
    {
        _contexto.PropuestasIngresos.Add(propuesta);
        await _contexto.SaveChangesAsync(tokenCancelacion);
    }

    // Implementa IRepositorioPropuestasIngresos.ObtenerAsync.
    public Task<PropuestaIngresos?> ObtenerAsync(Guid propuestaId, CancellationToken tokenCancelacion = default)
        => _contexto.PropuestasIngresos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == propuestaId, tokenCancelacion);

    // Implementa IRepositorioPropuestasIngresos.ListarAsync.
    public async Task<IReadOnlyList<PropuestaIngresos>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => (await _contexto.PropuestasIngresos.AsNoTracking().Where(x => x.PeriodoId == periodoId)
            .ToArrayAsync(tokenCancelacion)).OrderByDescending(x => x.Fecha).ThenBy(x => x.Id).ToArray();

    // Implementa IRepositorioPropuestasIngresos.ListarOcupacionesAsync: solo plazas activas, no propuestas.
    public async Task<IReadOnlyList<OcupacionPlaza>> ListarOcupacionesAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => await _contexto.OcupacionesPlazas.AsNoTracking().Where(x => x.PeriodoId == periodoId && x.Activa)
            .OrderBy(x => x.Id).ToArrayAsync(tokenCancelacion);
}
