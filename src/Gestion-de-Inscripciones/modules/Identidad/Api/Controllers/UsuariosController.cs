using Microsoft.AspNetCore.Mvc;
using SistemaAsignacionEscolar.Api.modules.Identidad.Application.Usuarios.Queries;
using System.Threading.Tasks;

namespace SistemaAsignacionEscolar.Api.modules.Identidad.Api.Controllers
{
    [ApiController]
    [Route("api/identidad/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly ConsultarUsuarioQueryHandler _consultarUsuarioQueryHandler;

        public UsuariosController(ConsultarUsuarioQueryHandler consultarUsuarioQueryHandler)
        {
            _consultarUsuarioQueryHandler = consultarUsuarioQueryHandler;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Consultar(int id)
        {
            var query = new ConsultarUsuarioQuery(id);
            var result = await _consultarUsuarioQueryHandler.ManejarAsync(query);
            
            if (result == null) 
                return NotFound();
                
            return Ok(result);
        }
    }
}
