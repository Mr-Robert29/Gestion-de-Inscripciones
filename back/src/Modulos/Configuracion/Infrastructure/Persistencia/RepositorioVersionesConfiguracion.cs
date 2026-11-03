using Jardin.Configuracion.Application.Persistencia;
using Jardin.Configuracion.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Infrastructure.Persistencia;

public sealed class RepositorioVersionesConfiguracion : IRepositorioVersionesConfiguracion
{
    private readonly ContextoConfiguracion _contexto;
    public RepositorioVersionesConfiguracion(ContextoConfiguracion contexto) { _contexto = contexto; }

    // Implementa IRepositorioVersionesConfiguracion.ObtenerAsync.
    public Task<VersionConfiguracion?> ObtenerAsync(Guid id, CancellationToken tokenCancelacion = default)
        => _contexto.VersionesConfiguracion.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, tokenCancelacion);

    // Implementa IRepositorioVersionesConfiguracion.ListarAsync.
    public async Task<IReadOnlyList<VersionConfiguracion>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        var versiones = await _contexto.VersionesConfiguracion.AsNoTracking().Where(v => v.PeriodoId == periodoId)
            .ToArrayAsync(tokenCancelacion);
        // El filtro se ejecuta en la base; el orden temporal también funciona en SQLite de pruebas.
        return versiones.OrderBy(v => v.Fecha).ThenBy(v => v.Id).ToArray();
    }

    // Implementa IRepositorioVersionesConfiguracion.AgregarAsync: publicación e historial son atómicos.
    public async Task AgregarAsync(
        VersionConfiguracion version,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.VersionesConfiguracion.Add(version);
        _contexto.Cambios.Add(cambio);
        await _contexto.SaveChangesAsync(tokenCancelacion);
    }
}
