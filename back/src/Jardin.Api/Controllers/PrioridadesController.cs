using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/periodos")]
public sealed class PrioridadesController : ControllerBase
{
    private readonly IModuloPrioridades _prioridades;

    public PrioridadesController(IModuloPrioridades prioridades)
    {
        _prioridades = prioridades;
    }

    [HttpGet("{periodoId:guid}/prioridades")]
    public Task<IActionResult> Consultar(
        Guid periodoId,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () =>
        {
            var estado = await _prioridades.ConsultarEstadoAsync(periodoId, cancellationToken);
            if (!estado.RecepcionCerrada)
                return Conflict(new ProblemDetails { Status = 409, Detail = "Las prioridades solo se consultan después de cerrar la recepción." });
            return Ok(estado.Vigente ? estado.Resultados : []);
        });

    [HttpGet("{periodoId:guid}/prioridades/estado")]
    public Task<IActionResult> Estado(
        Guid periodoId,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _prioridades.ConsultarEstadoAsync(periodoId, cancellationToken)));

    [HttpPost("{periodoId:guid}/prioridades/procesamiento")]
    public Task<IActionResult> Procesar(
        Guid periodoId,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _prioridades.ProcesarAsync(periodoId, cancellationToken)));

    [HttpGet("{periodoId:guid}/prioridades/historial")]
    public Task<IActionResult> Historial(
        Guid periodoId,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _prioridades.ConsultarHistorialAsync(periodoId, cancellationToken)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
