using Jardin.Secciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/periodos/{periodoId:guid}/secciones/{seccionId:guid}/composicion")]
public sealed class ComposicionSeccionesController(IModuloComposicionSecciones modulo) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Cambiar(
        Guid periodoId,
        Guid seccionId,
        CambiarComposicionSeccionPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        try
        {
            return Ok(await modulo.CambiarAsync(
                periodoId,
                seccionId,
                peticion,
                tokenCancelacion));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { mensaje = ex.Message }); }
    }

    [HttpGet("historial")]
    public async Task<IActionResult> Historial(
        Guid periodoId,
        Guid seccionId,
        CancellationToken tokenCancelacion)
    {
        try
        {
            return Ok(await modulo.ConsultarHistorialAsync(
                periodoId,
                seccionId,
                tokenCancelacion));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }
}
