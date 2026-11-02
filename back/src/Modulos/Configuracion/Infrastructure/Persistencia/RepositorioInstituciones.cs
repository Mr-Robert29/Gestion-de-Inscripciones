using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

public sealed class RepositorioInstituciones : IRepositorioInstituciones
{
    private readonly ContextoConfiguracion _contexto;

    public RepositorioInstituciones(ContextoConfiguracion contexto)
    {
        _contexto = contexto;
    }

    // Implementa IRepositorioInstituciones.ObtenerAsync.
    public Task<Institucion?> ObtenerAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        return _contexto.Instituciones.SingleOrDefaultAsync(i => i.Id == institucionId, tokenCancelacion);
    }

    // Implementa IRepositorioInstituciones.ListarAsync.
    public async Task<IReadOnlyList<Institucion>> ListarAsync(CancellationToken tokenCancelacion = default)
    {
        return await _contexto.Instituciones.AsNoTracking().OrderBy(i => i.Nombre).ToArrayAsync(tokenCancelacion);
    }

    // Implementa IRepositorioInstituciones.AgregarAsync: un solo SaveChanges conserva datos e historial juntos.
    public async Task AgregarAsync(
        Institucion institucion,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Instituciones.Add(institucion);
        _contexto.Cambios.Add(cambio);
        await _contexto.SaveChangesAsync(tokenCancelacion);
    }

    // Implementa IRepositorioInstituciones.ActualizarAsync: la revisión detecta escrituras concurrentes.
    public async Task ActualizarAsync(
        Institucion institucion,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        if (_contexto.Entry(institucion).State == EntityState.Detached)
            throw new InvalidOperationException("La institución debe consultarse en la misma operación antes de modificarla.");
        _contexto.Cambios.Add(cambio);
        try
        {
            await _contexto.SaveChangesAsync(tokenCancelacion);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoInstitucionException();
        }
    }

    // Implementa IRepositorioInstituciones.ConsultarHistorialAsync.
    public async Task<IReadOnlyList<CambioInstitucion>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        // La ordenación final permite ejecutar también las pruebas con SQLite.
        var cambios = await _contexto.Cambios.AsNoTracking().Where(c => c.InstitucionId == institucionId).ToArrayAsync(tokenCancelacion);
        return cambios.OrderByDescending(c => c.Fecha).ThenBy(c => c.Id).ToArray();
    }

    // Implementa IRepositorioInstituciones.RegistrarUsoAsync; repetir la misma referencia no duplica registros.
    public async Task RegistrarUsoAsync(UsoUbicacion uso, CancellationToken tokenCancelacion = default)
    {
        var existe = await _contexto.UsosUbicacion.AnyAsync(u =>
            u.InstitucionId == uso.InstitucionId && u.ReferenciaId == uso.ReferenciaId &&
            u.Tipo == uso.Tipo && u.RevisionUbicacion == uso.RevisionUbicacion, tokenCancelacion);
        if (existe) return;
        _contexto.UsosUbicacion.Add(uso);
        await _contexto.SaveChangesAsync(tokenCancelacion);
    }

    // Implementa IRepositorioInstituciones.ConsultarUsosAsync.
    public async Task<IReadOnlyList<UsoUbicacion>> ConsultarUsosAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        return await _contexto.UsosUbicacion.AsNoTracking().Where(u => u.InstitucionId == institucionId).ToArrayAsync(tokenCancelacion);
    }
}
