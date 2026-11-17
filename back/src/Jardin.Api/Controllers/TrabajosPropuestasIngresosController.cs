using Jardin.Api.Trabajos;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TrabajosPropuestasIngresosController(
    ColaPropuestasIngresos cola,
    IAutorizacionPropuestasIngresos autorizacion) : ControllerBase
{
    [HttpPost("api/v1/periodos/{periodoId:guid}/propuestas-ingresos/trabajos")]
    public async Task<IActionResult> Encolar(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(peticion);
            ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
            var autor = await autorizacion.AutorizarPropuestaAsync(periodoId, tokenCancelacion);
            if (!cola.Encolar(periodoId, peticion, autor, out var trabajoId))
                return StatusCode(503, new ProblemDetails { Status = 503, Detail = "No se pudo iniciar el cálculo. Intentá nuevamente." });
            return Accepted($"/api/v1/propuestas-ingresos/trabajos/{trabajoId}", new { id = trabajoId, estado = "En cola" });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    [HttpGet("api/v1/propuestas-ingresos/trabajos/{trabajoId:guid}")]
    public async Task<IActionResult> Consultar(Guid trabajoId, CancellationToken tokenCancelacion)
    {
        var trabajo = cola.Obtener(trabajoId);
        if (trabajo is null) return NotFound();
        try
        {
            var autor = await autorizacion.AutorizarPropuestaAsync(trabajo.PeriodoId, tokenCancelacion);
            if (autor.UsuarioId != trabajo.UsuarioId || autor.InstitucionId != trabajo.InstitucionId)
                return NotFound();
            return Ok(new
            {
                trabajo.Id,
                trabajo.Estado,
                trabajo.Mensaje,
                trabajo.PropuestaId,
                trabajo.ActualizadoEn
            });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
    }

    private static ObjectResult Error(int estado, string mensaje)
        => new(new ProblemDetails { Status = estado, Detail = mensaje }) { StatusCode = estado };
}
