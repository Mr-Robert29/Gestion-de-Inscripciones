using System.Threading.Tasks;
using SistemaEducativo.Modulos.Identidad.Domain.Repositories;

namespace SistemaEducativo.Modulos.Identidad.Application.Usuarios.Queries
{
    public class ConsultarUsuarioQueryHandler
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public ConsultarUsuarioQueryHandler(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<UsuarioDto> ManejarAsync(ConsultarUsuarioQuery query)
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(query.IdUsuario);

            if (usuario == null) return null;

            return new UsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
            };
        }
    }
}
