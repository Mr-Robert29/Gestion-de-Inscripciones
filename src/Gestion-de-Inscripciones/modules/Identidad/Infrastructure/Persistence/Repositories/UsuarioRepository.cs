using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repository;
using SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Contexts;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Infrastructure.Persistence.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly IdentidadDbContext _context;

        public UsuarioRepository(IdentidadDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario> ObtenerPorIdAsync(int idUsuario)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        }

        public async Task<Usuario> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task AgregarAsync(Usuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }
    }
}
