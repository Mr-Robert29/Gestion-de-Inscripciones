using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}")]
public sealed class SolicitudesController : ControllerBase
{
    private readonly IModuloSolicitudes _solicitudes;
    public SolicitudesController(IModuloSolicitudes solicitudes) { _solicitudes = solicitudes; }

    [HttpPost("periodos/{periodoId:guid}/solicitudes")]
    public Task<IActionResult> Registrar(
        Guid institucionId,
        Guid periodoId,
        RegistrarSolicitudPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _solicitudes.RegistrarAsync(
                institucionId,
                periodoId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(Consultar),
                new { institucionId, solicitudId = respuesta.Id },
                respuesta);
        });

    [HttpGet("solicitudes/{solicitudId:guid}")]
    public Task<IActionResult> Consultar(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _solicitudes.ConsultarAsync(
                institucionId,
                solicitudId,
                tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });

    [HttpGet("periodos/{periodoId:guid}/solicitudes")]
    public Task<IActionResult> Listar(
        Guid institucionId,
        Guid periodoId,
        [FromQuery] ListarSolicitudesPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _solicitudes.ListarAsync(
            institucionId,
            periodoId,
            peticion,
            tokenCancelacion)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
