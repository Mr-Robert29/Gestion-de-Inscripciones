using System.Threading.Tasks;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Usuarios.Queries
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
