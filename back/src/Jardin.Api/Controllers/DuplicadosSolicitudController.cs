using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}")]
public sealed class DuplicadosSolicitudController : ControllerBase
{
    private readonly IModuloDuplicadosSolicitud _duplicados;
    public DuplicadosSolicitudController(IModuloDuplicadosSolicitud duplicados) { _duplicados = duplicados; }

    [HttpPost("periodos/{periodoId:guid}/duplicados/detectar")]
    public Task<IActionResult> Detectar(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _duplicados.DetectarAsync(
            institucionId,
            periodoId,
            tokenCancelacion)));

    [HttpGet("periodos/{periodoId:guid}/duplicados")]
    public Task<IActionResult> Listar(
        Guid institucionId,
        Guid periodoId,
        [FromQuery] ListarDuplicadosPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _duplicados.ListarAsync(
            institucionId,
            periodoId,
            peticion,
            tokenCancelacion)));

    [HttpGet("duplicados/{alertaId:guid}")]
    public Task<IActionResult> Consultar(
        Guid institucionId,
        Guid alertaId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _duplicados.ConsultarAsync(
            institucionId,
            alertaId,
            tokenCancelacion)));

    [HttpPost("duplicados/{alertaId:guid}/resolver")]
    public Task<IActionResult> Resolver(
        Guid institucionId,
        Guid alertaId,
        ResolverDuplicadoPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _duplicados.ResolverAsync(
            institucionId,
            alertaId,
            peticion,
            tokenCancelacion)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (ConflictoDuplicadoException error) { return Problem(statusCode: 409, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
