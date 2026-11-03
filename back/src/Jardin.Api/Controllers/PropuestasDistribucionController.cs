using Jardin.Secciones.Contracts;
using Jardin.Usuarios.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(Roles = RolesPersonal.Directivo + "," + RolesPersonal.AdministradorDelSistema)]
public sealed class PropuestasDistribucionController : ControllerBase
{
    private readonly IModuloSecciones _modulo;

    public PropuestasDistribucionController(IModuloSecciones modulo)
    {
        _modulo = modulo;
    }

    [HttpPost("periodos/{periodoId:guid}/propuestas-distribucion")]
    public async Task<IActionResult> Generar(Guid periodoId, CancellationToken cancellationToken)
    {
        try
        {
            var propuesta = await _modulo.GenerarDistribucionAsync(periodoId, ObtenerUsuarioId(), cancellationToken);
            return Ok(propuesta);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpGet("propuestas-distribucion/{propuestaId:guid}")]
    public async Task<IActionResult> Obtener(Guid propuestaId, CancellationToken cancellationToken)
    {
        var propuesta = await _modulo.ObtenerPropuestaDistribucionAsync(propuestaId, cancellationToken);
        return propuesta is null ? NotFound() : Ok(propuesta);
    }

    [HttpPatch("propuestas-distribucion/{propuestaId:guid}/asignaciones")]
    public async Task<IActionResult> Modificar(Guid propuestaId, [FromBody] ModificarAsignacionPropuestaPeticion peticion, CancellationToken cancellationToken)
    {
        try
        {
            var usuarioId = ObtenerUsuarioId();
            return Ok(await _modulo.ModificarAsignacionAsync(propuestaId, peticion, usuarioId, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPost("propuestas-distribucion/{propuestaId:guid}/confirmaciones")]
    public async Task<IActionResult> Confirmar(Guid propuestaId, CancellationToken cancellationToken)
    {
        try
        {
            var propuesta = await _modulo.ConfirmarDistribucionAsync(propuestaId, ObtenerUsuarioId(), cancellationToken);
            return propuesta is null ? NotFound() : Ok(propuesta);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    private Guid ObtenerUsuarioId()
    {
        var claim = User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var usuarioId)
            ? usuarioId
            : throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");
    }
}
