using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Vacantes.Infrastructure.Persistencia;

public sealed class RepositorioOfrecimientos : IRepositorioOfrecimientos
{
    private readonly ContextoVacantes _contexto;
    public RepositorioOfrecimientos(ContextoVacantes contexto) { _contexto = contexto; }

    // Implementa IRepositorioOfrecimientos.ObtenerIngresoAsync; la lectura de comando es nueva en cada intento.
    public Task<IngresoConfirmado?> ObtenerIngresoAsync(Guid ingresoId, CancellationToken tokenCancelacion = default)
        => _contexto.IngresosConfirmados.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ingresoId, tokenCancelacion);

    // Implementa IRepositorioOfrecimientos.GuardarSolicitudBajaAsync: persiste el paso pendiente antes de llamar a Secciones.
    public async Task GuardarSolicitudBajaAsync(IngresoConfirmado ingreso, CancellationToken tokenCancelacion = default)
    {
        _contexto.IngresosConfirmados.Update(ingreso);
        _contexto.Entry(ingreso).Property(x => x.Revision).OriginalValue = ingreso.Revision - 1;
        await GuardarAsync(tokenCancelacion);
    }

    // Implementa IRepositorioOfrecimientos.CompletarBajaAsync: libera la plaza junto con la baja definitiva.
    public async Task<IngresoConfirmado> CompletarBajaAsync(Guid ingresoId, CancellationToken tokenCancelacion = default)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(tokenCancelacion);
        var ingreso = await _contexto.IngresosConfirmados.SingleAsync(x => x.Id == ingresoId, tokenCancelacion);
        if (!ingreso.Activo) return ingreso;
        var ocupacion = await _contexto.OcupacionesPlazas.SingleOrDefaultAsync(x =>
            x.PeriodoId == ingreso.PeriodoId && x.AspiranteId == ingreso.AspiranteId && x.Activa, tokenCancelacion);
        if (ocupacion is null || ocupacion.Tipo != "Ingreso" || ocupacion.OfertaId != ingreso.OfertaId)
            throw new InvalidOperationException("No se encuentra la ocupación activa coherente con el ingreso.");
        ingreso.CompletarBaja();
        ocupacion.Liberar();
        await GuardarAsync(tokenCancelacion);
        await transaccion.CommitAsync(tokenCancelacion);
        return ingreso;
    }

    // Implementa IRepositorioOfrecimientos.ObtenerAsync.
    public Task<OfrecimientoVacante?> ObtenerAsync(Guid ofrecimientoId, CancellationToken tokenCancelacion = default)
        => _contexto.Ofrecimientos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ofrecimientoId, tokenCancelacion);

    // Implementa IRepositorioOfrecimientos.BuscarOperacionAsync.
    public Task<OfrecimientoVacante?> BuscarOperacionAsync(Guid periodoId, Guid operacionId, CancellationToken tokenCancelacion = default)
        => _contexto.Ofrecimientos.AsNoTracking().SingleOrDefaultAsync(x => x.PeriodoId == periodoId && x.OperacionId == operacionId, tokenCancelacion);

    // Implementa IRepositorioOfrecimientos.ListarAsync.
    public async Task<IReadOnlyList<OfrecimientoVacante>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
        => (await _contexto.Ofrecimientos.AsNoTracking().Where(x => x.PeriodoId == periodoId).ToArrayAsync(tokenCancelacion))
            .OrderByDescending(x => x.Fecha).ThenBy(x => x.Id).ToArray();

    // Implementa IRepositorioOfrecimientos.HistorialAsync.
    public async Task<IReadOnlyList<CambioOfrecimiento>> HistorialAsync(Guid ofrecimientoId, CancellationToken tokenCancelacion = default)
        => (await _contexto.CambiosOfrecimientos.AsNoTracking().Where(x => x.OfrecimientoId == ofrecimientoId).ToArrayAsync(tokenCancelacion))
            .OrderBy(x => x.Fecha).ThenBy(x => x.Id).ToArray();

    // Implementa IRepositorioOfrecimientos.CrearAsync: ofrecimiento, reserva e historial forman una operación local atómica.
    public async Task CrearAsync(
        OfrecimientoVacante ofrecimiento,
        OcupacionPlaza reserva,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Ofrecimientos.Add(ofrecimiento);
        _contexto.OcupacionesPlazas.Add(reserva);
        _contexto.CambiosOfrecimientos.Add(new CambioOfrecimiento(ofrecimiento.Id, ofrecimiento.UsuarioId, "SinOfrecimiento", "Ofrecido", ofrecimiento.Motivo));
        await GuardarAsync(tokenCancelacion);
    }

    // Implementa IRepositorioOfrecimientos.GuardarRespuestaAsync: rechazo/cancelación liberan la reserva en la misma transacción.
    public async Task GuardarRespuestaAsync(
        OfrecimientoVacante ofrecimiento,
        CambioOfrecimiento cambio,
        CancellationToken tokenCancelacion = default)
    {
        var reserva = await ExigirReservaAsync(ofrecimiento, tokenCancelacion);
        if (ofrecimiento.Estado is "Rechazado" or "Cancelado") reserva.Liberar();
        Actualizar(ofrecimiento);
        _contexto.CambiosOfrecimientos.Add(cambio);
        await GuardarAsync(tokenCancelacion);
    }

    // Implementa IRepositorioOfrecimientos.ConfirmarAsync: convierte la reserva sin consumir otra plaza.
    public async Task ConfirmarAsync(
        OfrecimientoVacante ofrecimiento,
        IngresoConfirmado ingreso,
        CambioOfrecimiento cambio,
        CancellationToken tokenCancelacion = default)
    {
        var reserva = await ExigirReservaAsync(ofrecimiento, tokenCancelacion);
        reserva.ConfirmarIngreso();
        _contexto.IngresosConfirmados.Add(ingreso);
        Actualizar(ofrecimiento);
        _contexto.CambiosOfrecimientos.Add(cambio);
        await GuardarAsync(tokenCancelacion);
    }

    private void Actualizar(OfrecimientoVacante ofrecimiento)
    {
        _contexto.Ofrecimientos.Update(ofrecimiento);
        _contexto.Entry(ofrecimiento).Property(x => x.Revision).OriginalValue = ofrecimiento.Revision - 1;
    }

    private async Task<OcupacionPlaza> ExigirReservaAsync(OfrecimientoVacante ofrecimiento, CancellationToken tokenCancelacion)
    {
        var reserva = await _contexto.OcupacionesPlazas.SingleOrDefaultAsync(x => x.Id == ofrecimiento.ReservaId, tokenCancelacion);
        if (reserva is null || !reserva.Activa || reserva.Tipo != "Reserva" || reserva.AspiranteId != ofrecimiento.AspiranteId ||
            reserva.OfertaId != ofrecimiento.OfertaId || reserva.PeriodoId != ofrecimiento.PeriodoId)
            throw new InvalidOperationException("La reserva del ofrecimiento ya no está disponible.");
        return reserva;
    }

    private async Task GuardarAsync(CancellationToken tokenCancelacion)
    {
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateException error) { throw new InvalidOperationException("Otra operación modificó el ingreso, la reserva o el ofrecimiento. Volvé a consultar.", error); }
    }
}
