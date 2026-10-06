using Microsoft.AspNetCore.Mvc;
using SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Commands;
using SistemaAsignacionEscolar.Api.modules.Identidad.Application.Alumnos.Queries;
using System.Threading.Tasks;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Api.Controllers
{
    [ApiController]
    [Route("api/identidad/alumnos")]
    public class AlumnosController : ControllerBase
    {
        private readonly RegistrarAlumnoCommandHandler _registrarAlumnoCommandHandler;
        private readonly ConsultarAlumnoQueryHandler _consultarAlumnoQueryHandler;
        private readonly AsociarResponsableCommandHandler _asociarResponsableCommandHandler;

        public AlumnosController(
            RegistrarAlumnoCommandHandler registrarAlumnoCommandHandler,
            ConsultarAlumnoQueryHandler consultarAlumnoQueryHandler,
            AsociarResponsableCommandHandler asociarResponsableCommandHandler)
        {
            _registrarAlumnoCommandHandler = registrarAlumnoCommandHandler;
            _consultarAlumnoQueryHandler = consultarAlumnoQueryHandler;
            _asociarResponsableCommandHandler = asociarResponsableCommandHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] RegistrarAlumnoCommand command)
        {
            await _registrarAlumnoCommandHandler.ManejarAsync(command);
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Consultar(int id)
        {
            var query = new ConsultarAlumnoQuery(id);
            var result = await _consultarAlumnoQueryHandler.ManejarAsync(query);
            
            if (result == null) 
                return NotFound();
                
            return Ok(result);
        }

        [HttpPost("{id}/responsables")]
        public async Task<IActionResult> AsociarResponsable(int id, [FromBody] AsociarResponsableCommand command)
        {
            if (id != command.IdAlumno)
            {
                return BadRequest("El ID de la ruta no coincide con el ID del comando.");
            }
            
            await _asociarResponsableCommandHandler.ManejarAsync(command);
            return Ok();
        }
    }
}
