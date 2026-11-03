using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/periodos")]
public sealed class PeriodosController : ControllerBase
{
    private readonly IModuloPeriodos _periodos;
    public PeriodosController(IModuloPeriodos periodos) { _periodos = periodos; }

    [HttpPost("/api/v1/instituciones/{institucionId:guid}/periodos")]
    public Task<IActionResult> Crear(
        Guid institucionId,
        CrearPeriodoPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _periodos.CrearPeriodoAsync(
                institucionId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(nameof(Consultar), new { periodoId = respuesta.Id }, respuesta);
        });
    }

    [HttpGet("/api/v1/instituciones/{institucionId:guid}/periodos")]
    public Task<IActionResult> Listar(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _periodos.ListarPeriodosAsync(institucionId, tokenCancelacion)));
    }

    [HttpGet("{periodoId:guid}")]
    public Task<IActionResult> Consultar(Guid periodoId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _periodos.ConsultarPeriodoAsync(periodoId, tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });
    }

    [HttpPost("{periodoId:guid}/abrir")]
    public Task<IActionResult> Abrir(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _periodos.AbrirRecepcionAsync(
            periodoId,
            peticion,
            tokenCancelacion)));
    }

    [HttpPost("{periodoId:guid}/cerrar-recepcion")]
    public Task<IActionResult> Cerrar(
        Guid periodoId,
        CambiarRecepcionPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _periodos.CerrarRecepcionAsync(
            periodoId,
            peticion,
            tokenCancelacion)));
    }

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ConflictoInstitucionException error) { return Problem(statusCode: 409, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
