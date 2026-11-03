using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioSolicitudes : IRepositorioSolicitudes
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioSolicitudes(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioSolicitudes.AgregarAsync: solicitud, alternativas e historial se guardan juntos.
    public async Task AgregarAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Solicitudes.Add(solicitud);
        _contexto.Cambios.Add(cambio);
        await _contexto.SaveChangesAsync(tokenCancelacion);
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
}
