using Jardin.Usuarios.Application.Abstracciones;
using Jardin.Usuarios.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Usuarios.Infrastructure.Persistencia;

public class RepositorioUsuarios : IRepositorioUsuarios
{
    private readonly ContextoUsuarios _contexto;

    public RepositorioUsuarios(ContextoUsuarios contexto)
    {
        _contexto = contexto;
    }

    public async Task<UsuarioPersonal?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .Include(u => u.Accesos)
            .Include(u => u.IdentidadPersonal)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<UsuarioPersonal?> ObtenerPorCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .Include(u => u.Accesos)
            .Include(u => u.IdentidadPersonal)
            .FirstOrDefaultAsync(u => u.CorreoElectronico == correoElectronico, cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioPersonal>> ListarTodosAsync(CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .Include(u => u.Accesos)
            .Include(u => u.IdentidadPersonal)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(UsuarioPersonal usuario, CancellationToken cancellationToken = default)
    {
        await _contexto.UsuariosPersonal.AddAsync(usuario, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(UsuarioPersonal usuario, CancellationToken cancellationToken = default)
    {
        _contexto.UsuariosPersonal.Update(usuario);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarConAuditoriaAsync(UsuarioPersonal usuario, IReadOnlyCollection<EventoAuditoriaPersonal> eventos, CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        _contexto.UsuariosPersonal.Update(usuario);
        await _contexto.AuditoriaPersonal.AddRangeAsync(eventos, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }

    public async Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .AnyAsync(u => u.CorreoElectronico == correoElectronico, cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioPersonal>> ListarPorInstitucionAsync(Guid institucionId, CancellationToken cancellationToken = default)
        => await _contexto.UsuariosPersonal
            .Where(u => u.Accesos.Any(a => a.InstitucionId == institucionId))
            .Include(u => u.Accesos.Where(a => a.InstitucionId == institucionId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<IdentidadPersonal?> ObtenerIdentidadPorDniAsync(string dniNormalizado, CancellationToken cancellationToken = default)
        => _contexto.IdentidadesPersonal.FirstOrDefaultAsync(i => i.DniNormalizado == dniNormalizado, cancellationToken);

    public Task<IdentidadPersonal?> ObtenerIdentidadPorIdAsync(Guid identidadId, CancellationToken cancellationToken = default)
        => _contexto.IdentidadesPersonal.FirstOrDefaultAsync(i => i.Id == identidadId, cancellationToken);

    public async Task AgregarIdentidadAsync(IdentidadPersonal identidad, CancellationToken cancellationToken = default)
    {
        await _contexto.IdentidadesPersonal.AddAsync(identidad, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarIdentidadAsync(IdentidadPersonal identidad, CancellationToken cancellationToken = default)
    {
        _contexto.IdentidadesPersonal.Update(identidad);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioPersonal>> ListarPorIdentidadAsync(Guid identidadId, CancellationToken cancellationToken = default)
        => await _contexto.UsuariosPersonal
            .Where(u => u.IdentidadPersonalId == identidadId)
            .Include(u => u.Accesos)
            .ToListAsync(cancellationToken);

    public async Task ConsolidarIdentidadesAsync(Guid identidadOrigenId, IdentidadPersonal destino, CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancellationToken);
        var cuentas = await _contexto.UsuariosPersonal
            .Where(u => u.IdentidadPersonalId == identidadOrigenId)
            .ToListAsync(cancellationToken);
        foreach (var cuenta in cuentas) cuenta.ReasignarIdentidad(destino);
        var origen = await _contexto.IdentidadesPersonal.FindAsync([identidadOrigenId], cancellationToken);
        if (origen is not null) _contexto.IdentidadesPersonal.Remove(origen);
        await _contexto.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }

    public async Task RegistrarAuditoriaAsync(EventoAuditoriaPersonal evento, CancellationToken cancellationToken = default)
    {
        await _contexto.AuditoriaPersonal.AddAsync(evento, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventoAuditoriaPersonal>> ListarAuditoriaAsync(CancellationToken cancellationToken = default)
        => await _contexto.AuditoriaPersonal.AsNoTracking().OrderByDescending(e => e.FechaUtc).Take(500).ToListAsync(cancellationToken);
}
