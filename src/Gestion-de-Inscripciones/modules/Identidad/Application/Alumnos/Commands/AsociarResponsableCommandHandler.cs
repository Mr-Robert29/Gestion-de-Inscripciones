using System;
using System.Threading.Tasks;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Entities;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Commands
{
    public class AsociarResponsableCommandHandler
    {
        private readonly IAlumnoRepository _alumnoRepository;

        public AsociarResponsableCommandHandler(IAlumnoRepository alumnoRepository)
        {
            _alumnoRepository = alumnoRepository;
        }

        public async Task ManejarAsync(AsociarResponsableCommand comando)
        {
            var alumno = await _alumnoRepository.ObtenerPorIdAsync(comando.IdAlumno);

            if (alumno == null)
            {
                throw new Exception($"No se encontró el alumno con ID {comando.IdAlumno}");
            }

            var responsable = new Responsable(
                comando.Nombre,
                comando.Apellido,
                comando.TipoDocumento,
                comando.NroDocumento,
                comando.Telefono,
                comando.Email,
                comando.Vinculo,
                comando.EsTutor
            );

            alumno.AsociarResponsable(responsable);

            await _alumnoRepository.ActualizarAsync(alumno);
        }
    }
}
