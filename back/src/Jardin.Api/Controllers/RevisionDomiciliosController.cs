using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}/solicitudes/{solicitudId:guid}/revision-domicilio")]
public sealed class RevisionDomiciliosController : ControllerBase
{
    private readonly IModuloRevisionDomicilios _revisiones;
    public RevisionDomiciliosController(IModuloRevisionDomicilios revisiones) { _revisiones = revisiones; }

    [HttpGet]
    public Task<IActionResult> Consultar(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _revisiones.ConsultarAsync(
            institucionId,
            solicitudId,
            tokenCancelacion)));

    [HttpPost]
    public Task<IActionResult> Registrar(
        Guid institucionId,
        Guid solicitudId,
        RegistrarRevisionDomicilioPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _revisiones.RegistrarAsync(
                institucionId,
                solicitudId,
                peticion,
                tokenCancelacion);
            return Created($"/api/v1/instituciones/{institucionId}/solicitudes/{solicitudId}/revision-domicilio", respuesta);
        });

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
