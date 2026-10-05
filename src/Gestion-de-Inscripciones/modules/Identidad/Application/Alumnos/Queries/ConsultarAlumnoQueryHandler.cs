using System;
using System.Threading.Tasks;
using SistemaAsignacionEscolar.Api.modules.Identidad.Domain.Repositories;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Queries
{
    public class ConsultarAlumnoQueryHandler
    {
        private readonly IAlumnoRepository _alumnoRepository;

        public ConsultarAlumnoQueryHandler(IAlumnoRepository alumnoRepository)
        {
            _alumnoRepository = alumnoRepository;
        }

        public async Task<AlumnoDto> ManejarAsync(ConsultarAlumnoQuery query)
        {
            var alumno = await _alumnoRepository.ObtenerPorIdAsync(query.IdAlumno);

            if (alumno == null) return null;

            return new AlumnoDto
            {
                IdAlumno = alumno.IdAlumno,
                NombreCompleto = $"{alumno.Nombre} {alumno.Apellido}",
                NroDocumento = alumno.NroDocumento,
                Edad = alumno.ObtenerGrupoEtario(DateTime.Now)
            };
        }
    }
}
