using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioAntecedentesSolicitud : IRepositorioAntecedentesSolicitud
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioAntecedentesSolicitud(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioAntecedentesSolicitud.ObtenerAsync: datos y revisión se leen juntos.
    public Task<SolicitudInscripcion?> ObtenerAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
        => _contexto.Solicitudes.AsSingleQuery().Include(s => s.Documentos).Include(s => s.Condiciones)
            .SingleOrDefaultAsync(s => s.Id == solicitudId && s.InstitucionId == institucionId, tokenCancelacion);

    // Implementa IRepositorioAntecedentesSolicitud.AgregarDocumentoAsync.
    public Task AgregarDocumentoAsync(
        SolicitudInscripcion solicitud,
        DocumentacionSolicitud documento,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        ComprobarSeguimiento(solicitud);
        _contexto.DocumentosSolicitud.Add(documento);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    // Implementa IRepositorioAntecedentesSolicitud.AgregarCondicionAsync.
    public Task AgregarCondicionAsync(
        SolicitudInscripcion solicitud,
        CondicionPrioridad condicion,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        ComprobarSeguimiento(solicitud);
        _contexto.CondicionesPrioridad.Add(condicion);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    // Implementa IRepositorioAntecedentesSolicitud.GuardarRevisionAsync.
    public Task GuardarRevisionAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        ComprobarSeguimiento(solicitud);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    private void ComprobarSeguimiento(SolicitudInscripcion solicitud)
    {
        if (_contexto.Entry(solicitud).State == EntityState.Detached)
            throw new InvalidOperationException("Consultá la solicitud antes de modificar sus antecedentes.");
    }

    private async Task GuardarAsync(CambioInscripciones cambio, CancellationToken tokenCancelacion)
    {
        _contexto.Cambios.Add(cambio);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoRevisionSolicitudException(); }
    }
}
