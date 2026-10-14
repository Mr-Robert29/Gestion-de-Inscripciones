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
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<UsuarioPersonal?> ObtenerPorCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .Include(u => u.Accesos)
            .FirstOrDefaultAsync(u => u.CorreoElectronico == correoElectronico, cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioPersonal>> ListarTodosAsync(CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .Include(u => u.Accesos)
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

    public async Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default)
    {
        return await _contexto.UsuariosPersonal
            .AnyAsync(u => u.CorreoElectronico == correoElectronico, cancellationToken);
    }
}
