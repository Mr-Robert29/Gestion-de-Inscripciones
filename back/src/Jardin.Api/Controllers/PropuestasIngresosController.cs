using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class PropuestasIngresosController : ControllerBase
{
    private readonly IModuloPropuestasIngresos _propuestas;

    public PropuestasIngresosController(IModuloPropuestasIngresos propuestas) { _propuestas = propuestas; }

    [HttpPost("api/v1/periodos/{periodoId:guid}/propuestas-ingresos")]
    public Task<IActionResult> Generar(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var propuesta = await _propuestas.GenerarAsync(periodoId, peticion, tokenCancelacion);
            return Created($"/api/v1/propuestas-ingresos/{propuesta.Id}", propuesta);
        });

    [HttpGet("api/v1/periodos/{periodoId:guid}/propuestas-ingresos")]
    public Task<IActionResult> Listar(Guid periodoId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _propuestas.ListarAsync(periodoId, tokenCancelacion)));

    [HttpGet("api/v1/propuestas-ingresos/{propuestaId:guid}")]
    public Task<IActionResult> Consultar(Guid propuestaId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var propuesta = await _propuestas.ConsultarAsync(propuestaId, tokenCancelacion);
            return propuesta is null ? NotFound() : Ok(propuesta);
        });

    private static async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return new ForbidResult(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    private static ObjectResult Error(int estado, string mensaje)
        => new(new ProblemDetails { Status = estado, Detail = mensaje }) { StatusCode = estado };
}
