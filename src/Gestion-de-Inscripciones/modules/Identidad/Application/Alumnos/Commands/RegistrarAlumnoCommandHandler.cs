using System.Threading.Tasks;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Commands
{
    public class RegistrarAlumnoCommandHandler
    {
        private readonly IAlumnoRepository _alumnoRepository;

        public RegistrarAlumnoCommandHandler(IAlumnoRepository alumnoRepository)
        {
            _alumnoRepository = alumnoRepository;
        }

        public async Task ManejarAsync(RegistrarAlumnoCommand comando)
        {
            var alumno = new Alumno(
                comando.Nombre, 
                comando.Apellido, 
                comando.TipoDocumento, 
                comando.NroDocumento, 
                comando.FechaNacimiento, 
                comando.Sexo
            );

            await _alumnoRepository.AgregarAsync(alumno);
        }
    }
}
