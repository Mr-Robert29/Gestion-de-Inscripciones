using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using System.Threading.Tasks;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario> ObtenerPorIdAsync(int idUsuario);

        // Fundamental para el login o identificaci�n del usuario
        Task<Usuario> ObtenerPorEmailAsync(string email);

        Task AgregarAsync(Usuario usuario);

        Task ActualizarAsync(Usuario usuario);
    }
}
