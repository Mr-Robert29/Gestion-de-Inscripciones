using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task ActualizarEstadoAsync(Guid propuestaId, EstadoPropuestaIngreso estado, int revisionEsperada, CancellationToken tokenCancelacion = default)
    {
        var propuesta = await _contexto.PropuestasIngresos.SingleOrDefaultAsync(x => x.Id == propuestaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la propuesta.");
        propuesta.CambiarEstado(estado, revisionEsperada);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("La propuesta cambió mientras se la estaba editando."); }
    }

    // Implementa IRepositorioPropuestasIngresos.ListarOcupacionesAsync: solo plazas activas, no propuestas.
    public async Task<IReadOnlyList<OcupacionPlaza>> ListarOcupacionesAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => await _contexto.OcupacionesPlazas.AsNoTracking().Where(x => x.PeriodoId == periodoId && x.Activa)
            .OrderBy(x => x.Id).ToArrayAsync(tokenCancelacion);

    public Task<AceptacionTurnoFamiliar?> ObtenerAceptacionAsync(Guid propuestaId, Guid solicitudId, Guid turnoId, CancellationToken tokenCancelacion = default)
        => _contexto.AceptacionesTurno.AsNoTracking().SingleOrDefaultAsync(
            x => x.PropuestaId == propuestaId && x.SolicitudId == solicitudId && x.TurnoId == turnoId,
            tokenCancelacion);

    public async Task GuardarAceptacionAsync(AceptacionTurnoFamiliar aceptacion, CancellationToken tokenCancelacion = default)
    {
        _contexto.AceptacionesTurno.Add(aceptacion);
        await _contexto.SaveChangesAsync(tokenCancelacion);
    }

    public Task<IngresoConfirmado?> ObtenerIngresoAsync(Guid periodoId, Guid aspiranteId, CancellationToken tokenCancelacion = default)
        => _contexto.IngresosConfirmados.AsNoTracking().SingleOrDefaultAsync(
            x => x.PeriodoId == periodoId && x.AspiranteId == aspiranteId,
            tokenCancelacion);

    public async Task<IReadOnlyList<IngresoConfirmado>> ListarIngresosActivosAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => (await _contexto.IngresosConfirmados.AsNoTracking()
            .Where(x => x.PeriodoId == periodoId)
            .ToArrayAsync(tokenCancelacion)).OrderBy(x => x.Fecha).ThenBy(x => x.Id).ToArray();

    public async Task<IReadOnlyList<IngresoConfirmado>> GuardarConfirmacionesAsync(IReadOnlyList<IngresoConfirmado> ingresos, CancellationToken tokenCancelacion = default)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(tokenCancelacion);
        var confirmados = new List<IngresoConfirmado>();
        foreach (var ingreso in ingresos)
        {
            var existente = await _contexto.IngresosConfirmados.SingleOrDefaultAsync(
                x => x.PeriodoId == ingreso.PeriodoId && x.AspiranteId == ingreso.AspiranteId,
                tokenCancelacion);
            if (existente is not null)
            {
                confirmados.Add(existente);
                continue;
            }

            var ocupacion = await _contexto.OcupacionesPlazas.SingleOrDefaultAsync(
                x => x.PeriodoId == ingreso.PeriodoId && x.AspiranteId == ingreso.AspiranteId && x.Activa,
                tokenCancelacion);
            if (ocupacion is null)
                _contexto.OcupacionesPlazas.Add(new OcupacionPlaza(
                    ingreso.PeriodoId,
                    ingreso.AspiranteId,
                    ingreso.OfertaId,
                    ingreso.Sala,
                    "Ingreso"));
            else
            {
                if (ocupacion.OfertaId != ingreso.OfertaId)
                    throw new InvalidOperationException("El aspirante ya ocupa otra plaza activa.");
                ocupacion.ConfirmarIngreso();
            }

            _contexto.IngresosConfirmados.Add(ingreso);
            confirmados.Add(ingreso);
        }
        try
        {
            await _contexto.SaveChangesAsync(tokenCancelacion);
            await transaccion.CommitAsync(tokenCancelacion);
            return confirmados;
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "23505" })
        {
            await transaccion.RollbackAsync(tokenCancelacion);
            _contexto.ChangeTracker.Clear();
            var periodoId = ingresos[0].PeriodoId;
            var aspirantes = ingresos.Select(x => x.AspiranteId).ToArray();
            var existentes = await _contexto.IngresosConfirmados.AsNoTracking()
                .Where(x => x.PeriodoId == periodoId && aspirantes.Contains(x.AspiranteId))
                .ToArrayAsync(tokenCancelacion);
            if (existentes.Length == ingresos.Count)
                return existentes;
            throw new InvalidOperationException("Otra operación modificó las plazas durante la confirmación.", error);
        }
    }
}
