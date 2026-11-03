using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioDistancias : IRepositorioDistancias
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioDistancias(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioDistancias.GuardarAsync: registro, revisión de solicitud, referencia e historial son atómicos.
    public async Task GuardarAsync(
        MedicionDistancia medicion,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        var solicitud = await _contexto.Solicitudes.SingleAsync(s => s.Id == medicion.SolicitudId, tokenCancelacion);
        var ultimo = await _contexto.MedicionesDistancia.Where(m => m.SolicitudId == medicion.SolicitudId)
            .Select(m => (int?)m.NumeroRegistro).MaxAsync(tokenCancelacion) ?? 0;
        if (solicitud.InstitucionId != medicion.InstitucionId || solicitud.RevisionDomicilio != medicion.RevisionDomicilio ||
            medicion.NumeroRegistro != ultimo + 1)
            throw new ConflictoRevisionSolicitudException();
        solicitud.RegistrarRevisionAntecedentes();
        _contexto.MedicionesDistancia.Add(medicion);
        _contexto.Cambios.Add(cambio);
        _contexto.UsosDatosSolicitud.Add(new UsoDatosSolicitud(
            medicion.SolicitudId,
            medicion.Id,
            "MedicionDistancia",
            solicitud.RevisionDatos,
            medicion.RevisionDomicilio));
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoRevisionSolicitudException(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "23505", ConstraintName: "UX_Distancia_Solicitud_Numero" })
        { throw new ConflictoRevisionSolicitudException(); }
    }

    // Implementa IRepositorioDistancias.ConsultarAsync: devuelve todo el historial en orden de registro.
    public async Task<IReadOnlyList<MedicionDistancia>> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
        => await _contexto.MedicionesDistancia.AsNoTracking()
            .Where(m => m.InstitucionId == institucionId && m.SolicitudId == solicitudId)
            .OrderByDescending(m => m.NumeroRegistro).ToArrayAsync(tokenCancelacion);
}
