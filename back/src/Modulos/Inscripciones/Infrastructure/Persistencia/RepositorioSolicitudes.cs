using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioSolicitudes : IRepositorioSolicitudes
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioSolicitudes(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioSolicitudes.AgregarAsync: solicitud, alternativas, alertas e historial se guardan juntos.
    public async Task<int> AgregarAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Solicitudes.Add(solicitud);
        _contexto.Cambios.Add(cambio);
        var alertas = await new RegistroDeteccionDuplicados(_contexto).PrepararNuevaSolicitudAsync(
            solicitud,
            cambio.UsuarioId,
            tokenCancelacion);
        await _contexto.SaveChangesAsync(tokenCancelacion);
        return alertas;
    }

    // Implementa IRepositorioSolicitudes.ObtenerAsync.
    public Task<SolicitudInscripcion?> ObtenerAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
        => _contexto.Solicitudes.AsNoTracking().AsSingleQuery().Include(s => s.TurnosAlternativos)
            .SingleOrDefaultAsync(s => s.InstitucionId == institucionId && s.Id == solicitudId, tokenCancelacion);

    // Implementa IRepositorioSolicitudes.ListarAsync: orden de consulta estable, no un orden de admisión.
    public async Task<IReadOnlyList<SolicitudInscripcion>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default)
        => await _contexto.Solicitudes.AsNoTracking().AsSingleQuery().Include(s => s.TurnosAlternativos)
            .Where(s => s.InstitucionId == institucionId && s.PeriodoId == periodoId)
            .OrderBy(s => s.Id).Skip(desplazamiento).Take(cantidad).ToArrayAsync(tokenCancelacion);

    public async Task<IReadOnlyList<SolicitudEvaluableInterna>> ListarEvaluablesAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default)
    {
        var solicitudes = await _contexto.Solicitudes
            .AsNoTracking()
            .AsSingleQuery()
            .Include(s => s.Condiciones)
            .Where(s => s.InstitucionId == institucionId && s.PeriodoId == periodoId)
            .OrderBy(s => s.Id)
            .ToArrayAsync(tokenCancelacion);
        var ids = solicitudes.Select(s => s.Id).ToArray();
        var distancias = await _contexto.MedicionesDistancia
            .AsNoTracking()
            .Where(m => ids.Contains(m.SolicitudId))
            .OrderByDescending(m => m.NumeroRegistro)
            .ToArrayAsync(tokenCancelacion);

        return solicitudes.Select(s =>
        {
            var distancia = distancias.FirstOrDefault(m =>
                m.SolicitudId == s.Id && m.RevisionDomicilio == s.RevisionDomicilio);
            return new SolicitudEvaluableInterna(
                s.Id,
                s.AspiranteId,
                s.InstitucionId,
                s.PeriodoId,
                s.CalculoEdad.Sala,
                s.FueraDeTermino,
                s.Condiciones.ToArray(),
                distancia?.DistanciaMetros,
                s.RevisionDatos,
                s.RevisionDomicilio);
        }).ToArray();
    }
    public async Task<IReadOnlyList<ParticipanteSorteoInterno>> ListarParticipantesSorteoAsync(
        Guid institucionId,
        Guid periodoId,
        int sala,
        CancellationToken tokenCancelacion = default)
        => await (
            from solicitud in _contexto.Solicitudes.AsNoTracking()
            join aspirante in _contexto.Aspirantes.AsNoTracking()
                on solicitud.AspiranteId equals aspirante.Id
            where solicitud.InstitucionId == institucionId &&
                  solicitud.PeriodoId == periodoId &&
                  solicitud.CalculoEdad.Sala == sala &&
                  solicitud.EstadoRecepcion == EstadoRecepcionSolicitud.Ordinaria
            orderby solicitud.Id
            select new ParticipanteSorteoInterno(
                solicitud.Id,
                solicitud.AspiranteId,
                solicitud.CalculoEdad.Sala!.Value,
                aspirante.Dni))
            .ToArrayAsync(tokenCancelacion);
}
