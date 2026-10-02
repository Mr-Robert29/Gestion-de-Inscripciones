using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using System.Threading.Tasks;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repository
{
    public interface IUsuarioRepository
    {
        Task<Usuario> ObtenerPorIdAsync(int idUsuario);

        // Fundamental para el login o identificación del usuario
        Task<Usuario> ObtenerPorEmailAsync(string email);

        Task AgregarAsync(Usuario usuario);

        Task ActualizarAsync(Usuario usuario);
    }
}