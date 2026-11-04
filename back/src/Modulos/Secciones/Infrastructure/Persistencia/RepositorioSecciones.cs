using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Secciones.Infrastructure.Persistencia;

public sealed class RepositorioSecciones : IRepositorioSecciones
{
    private readonly ContextoSecciones _contexto;

    public RepositorioSecciones(ContextoSecciones contexto)
    {
        _contexto = contexto;
    }

    // Implementa IRepositorioSecciones.AgregarAsync con protección de la oferta compartida.
    public async Task AgregarAsync(Seccion seccion, CancellationToken cancellationToken = default)
    {
        await ProteccionSecciones.EjecutarAsync(
            _contexto,
            ProteccionSecciones.Oferta(seccion.PeriodoId, seccion.AulaId, seccion.TurnoId),
            async () => { await _contexto.Secciones.AddAsync(seccion, cancellationToken); await _contexto.SaveChangesAsync(cancellationToken); return true; },
            cancellationToken);
    }

    public Task<Seccion?> ObtenerPorIdAsync(Guid seccionId, CancellationToken cancellationToken = default) =>
        _contexto.Secciones.FirstOrDefaultAsync(x => x.Id == seccionId, cancellationToken);

    public async Task<IReadOnlyList<Seccion>> ListarPorPeriodoAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default) =>
        await _contexto.Secciones
            .AsNoTracking()
            .Where(x => x.PeriodoId == periodoId && x.Activa)
            .OrderBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

    // Implementa IRepositorioSecciones.ActualizarAsync con protección de la composición leída por Vacantes.
    public async Task ActualizarAsync(Seccion seccion, CancellationToken cancellationToken = default)
    {
        await ProteccionSecciones.EjecutarAsync(
            _contexto,
            ProteccionSecciones.Oferta(seccion.PeriodoId, seccion.AulaId, seccion.TurnoId),
            async () => { _contexto.Secciones.Update(seccion); await _contexto.SaveChangesAsync(cancellationToken); return true; },
            cancellationToken);
    }
}
