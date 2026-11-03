using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones")]
public sealed class InstitucionesController : ControllerBase
{
    private readonly IModuloConfiguracion _configuracion;

    public InstitucionesController(IModuloConfiguracion configuracion)
    {
        _configuracion = configuracion;
    }

    [HttpPost]
    public Task<IActionResult> Registrar(RegistrarInstitucionPeticion peticion, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _configuracion.RegistrarInstitucionAsync(peticion, tokenCancelacion);
            return CreatedAtAction(nameof(Consultar), new { institucionId = respuesta.Id }, respuesta);
        });
    }

    [HttpGet]
    public Task<IActionResult> Listar(CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _configuracion.ListarInstitucionesAsync(tokenCancelacion)));
    }

    [HttpGet("{institucionId:guid}")]
    public Task<IActionResult> Consultar(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _configuracion.ConsultarInstitucionAsync(institucionId, tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });
    }

    [HttpPut("{institucionId:guid}")]
    public Task<IActionResult> Actualizar(
        Guid institucionId,
        ActualizarInstitucionPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _configuracion.ActualizarInstitucionAsync(
            institucionId,
            peticion,
            tokenCancelacion)));
    }

    [HttpGet("{institucionId:guid}/historial")]
    public Task<IActionResult> ConsultarHistorial(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _configuracion.ConsultarHistorialAsync(institucionId, tokenCancelacion)));
    }

    [HttpGet("{institucionId:guid}/impacto-ubicacion")]
    public Task<IActionResult> ConsultarImpacto(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _configuracion.ConsultarImpactoUbicacionAsync(institucionId, tokenCancelacion)));
    }

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ConflictoInstitucionException error) { return Problem(statusCode: 409, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
    }
}
