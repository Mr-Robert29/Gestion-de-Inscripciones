using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using System.Threading.Tasks;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories
{
    public interface IAlumnoRepository
    {
        // Obtiene el alumno con todos sus datos internos cargados
        Task<Alumno> ObtenerPorIdAsync(int idAlumno);

        // Obtiene el alumno a través de su DNI para validaciones de existencia
        Task<Alumno> ObtenerPorDocumentoAsync(string nroDocumento);

        // Persiste un alumno nuevo en el sistema
        Task AgregarAsync(Alumno alumno);

        // Actualiza los cambios (como un nuevo responsable o cambio de domicilio)
        Task ActualizarAsync(Alumno alumno);
    }
}
