using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioDuplicadosSolicitud : IRepositorioDuplicadosSolicitud
{
    private readonly ContextoInscripciones _contexto;
    private readonly RegistroDeteccionDuplicados _registro;
    public RepositorioDuplicadosSolicitud(ContextoInscripciones contexto)
    {
        _contexto = contexto;
        _registro = new RegistroDeteccionDuplicados(contexto);
    }

    // Implementa IRepositorioDuplicadosSolicitud.DetectarAsync: el llamador mantiene protegido el período.
    public async Task<DetectarDuplicadosRespuesta> DetectarAsync(
        Guid institucionId,
        Guid periodoId,
        Guid autor,
        CancellationToken tokenCancelacion = default)
    {
        var candidatas = await _registro.Identificaciones(institucionId, periodoId).ToArrayAsync(tokenCancelacion);
        var identidades = candidatas.ToDictionary(s => s.SolicitudId);
        var existentes = await _contexto.PosiblesDuplicados.Where(a => a.InstitucionId == institucionId && a.PeriodoId == periodoId)
            .ToArrayAsync(tokenCancelacion);
        var porPareja = existentes.ToDictionary(a => (a.SolicitudAId, a.SolicitudBId));
        var parejas = DetectorPosiblesDuplicados.BuscarParejas(candidatas);
        // Revisa también alertas históricas cuyos datos hayan dejado de coincidir, sin descartarlas automáticamente.
        parejas.UnionWith(porPareja.Keys);
        var nuevas = 0;
        var actualizadas = 0;
        foreach (var (aId, bId) in parejas)
        {
            if (!identidades.TryGetValue(aId, out var a) || !identidades.TryGetValue(bId, out var b))
                throw new InvalidOperationException("Una alerta referencia solicitudes que ya no están disponibles en su contexto.");
            if (porPareja.TryGetValue((aId, bId), out var alerta))
            {
                var anteriores = RegistroDeteccionDuplicados.Serializar(alerta);
                if (!alerta.ActualizarDeteccion(
                    a,
                    b,
                    autor)) continue;
                _contexto.Cambios.Add(RegistroDeteccionDuplicados.CrearCambio(
                    alerta,
                    "Duplicado.DeteccionActualizada",
                    autor,
                    anteriores));
                actualizadas++;
            }
            else
            {
                _registro.Agregar(new PosibleDuplicado(
                    a,
                    b,
                    autor));
                nuevas++;
            }
        }
        if (nuevas + actualizadas > 0)
        {
            try { await _contexto.SaveChangesAsync(tokenCancelacion); }
            catch (DbUpdateConcurrencyException) { throw new ConflictoDuplicadoException(); }
        }
        return new DetectarDuplicadosRespuesta(nuevas, actualizadas);
    }

    // Implementa IRepositorioDuplicadosSolicitud.ObtenerAsync.
    public Task<PosibleDuplicado?> ObtenerAsync(
        Guid institucionId,
        Guid alertaId,
        CancellationToken tokenCancelacion = default)
        => _contexto.PosiblesDuplicados.SingleOrDefaultAsync(a => a.Id == alertaId && a.InstitucionId == institucionId, tokenCancelacion);

    // Implementa IRepositorioDuplicadosSolicitud.ListarAsync.
    public async Task<IReadOnlyList<PosibleDuplicado>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        EstadoPosibleDuplicado? estado,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default)
    {
        var consulta = _contexto.PosiblesDuplicados.AsNoTracking().Where(a => a.InstitucionId == institucionId && a.PeriodoId == periodoId);
        if (estado.HasValue) consulta = consulta.Where(a => a.Estado == estado);
        return await consulta.OrderBy(a => a.Id).Skip(desplazamiento).Take(cantidad).ToArrayAsync(tokenCancelacion);
    }

    // Implementa IRepositorioDuplicadosSolicitud.LeerParejaActualAsync.
    public async Task<IReadOnlyList<IdentificacionSolicitud>> LeerParejaActualAsync(PosibleDuplicado alerta, CancellationToken tokenCancelacion = default)
        => await _registro.Identificaciones(
            alerta.InstitucionId,
            alerta.PeriodoId,
            [alerta.SolicitudAId, alerta.SolicitudBId]).ToArrayAsync(tokenCancelacion);

    // Implementa IRepositorioDuplicadosSolicitud.GuardarResolucionAsync.
    public async Task GuardarResolucionAsync(
        PosibleDuplicado alerta,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        if (_contexto.Entry(alerta).State == EntityState.Detached) throw new InvalidOperationException("Consultá la alerta antes de resolverla.");
        _contexto.Cambios.Add(cambio);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoDuplicadoException(); }
    }
}
