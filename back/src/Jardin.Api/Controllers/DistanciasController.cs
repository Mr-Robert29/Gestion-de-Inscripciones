using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}/solicitudes/{solicitudId:guid}/distancias")]
public sealed class DistanciasController : ControllerBase
{
    private readonly IModuloDistancias _distancias;
    public DistanciasController(IModuloDistancias distancias) { _distancias = distancias; }

    [HttpPost("manual")]
    public Task<IActionResult> RegistrarManual(
        Guid institucionId,
        Guid solicitudId,
        RegistrarDistanciaManualPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var resultado = await _distancias.RegistrarManualAsync(
                institucionId,
                solicitudId,
                peticion,
                tokenCancelacion);
            return Created($"/api/v1/instituciones/{institucionId}/solicitudes/{solicitudId}/distancias", resultado);
        });

    [HttpGet]
    public Task<IActionResult> ConsultarHistorial(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _distancias.ConsultarHistorialAsync(
            institucionId,
            solicitudId,
            tokenCancelacion)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (ConflictoRevisionSolicitudException error) { return Problem(statusCode: 409, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
