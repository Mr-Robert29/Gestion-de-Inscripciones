using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioRevisionDomicilios : IRepositorioRevisionDomicilios
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioRevisionDomicilios(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioRevisionDomicilios.ConsultarAsync: conserva las decisiones de direcciones anteriores.
    public async Task<IReadOnlyList<RevisionManualDomicilio>> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
        => await _contexto.RevisionesDomicilio.AsNoTracking().Where(r => r.InstitucionId == institucionId && r.SolicitudId == solicitudId)
            .OrderByDescending(r => r.NumeroRegistro).ToArrayAsync(tokenCancelacion);

    // Implementa IRepositorioRevisionDomicilios.GuardarAsync: decisión, revisión de solicitud e historial se guardan juntos.
    public async Task GuardarAsync(
        RevisionManualDomicilio revision,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        var solicitud = await _contexto.Solicitudes.SingleAsync(s => s.Id == revision.SolicitudId, tokenCancelacion);
        var ultimo = await _contexto.RevisionesDomicilio.Where(r => r.SolicitudId == revision.SolicitudId)
            .Select(r => (int?)r.NumeroRegistro).MaxAsync(tokenCancelacion) ?? 0;
        if (solicitud.InstitucionId != revision.InstitucionId || solicitud.RevisionDatos != revision.RevisionSolicitudUtilizada ||
            solicitud.RevisionDomicilio != revision.RevisionDomicilio || revision.NumeroRegistro != ultimo + 1)
            throw new ConflictoRevisionSolicitudException();
        solicitud.RegistrarRevisionAntecedentes();
        _contexto.RevisionesDomicilio.Add(revision);
        _contexto.Cambios.Add(cambio);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoRevisionSolicitudException(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "23505", ConstraintName: "UX_RevisionDomicilio_Solicitud_Numero" })
        { throw new ConflictoRevisionSolicitudException(); }
    }
}
