using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}")]
public sealed class PersonasInscripcionesController : ControllerBase
{
    private readonly IModuloPersonasInscripciones _personas;
    public PersonasInscripcionesController(IModuloPersonasInscripciones personas) { _personas = personas; }

    [HttpPost("aspirantes")]
    public Task<IActionResult> RegistrarAspirante(
        Guid institucionId,
        RegistrarAspirantePeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _personas.RegistrarAspiranteAsync(
                institucionId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(ConsultarAspirante),
                new { institucionId, aspiranteId = respuesta.Id },
                respuesta);
        });

    [HttpGet("aspirantes/{aspiranteId:guid}")]
    public Task<IActionResult> ConsultarAspirante(
        Guid institucionId,
        Guid aspiranteId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _personas.ConsultarAspiranteAsync(
                institucionId,
                aspiranteId,
                tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });

    [HttpPost("responsables")]
    public Task<IActionResult> RegistrarResponsable(
        Guid institucionId,
        RegistrarResponsablePeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _personas.RegistrarResponsableAsync(
                institucionId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(ConsultarResponsable),
                new { institucionId, responsableId = respuesta.Id },
                respuesta);
        });

    [HttpGet("responsables/{responsableId:guid}")]
    public Task<IActionResult> ConsultarResponsable(
        Guid institucionId,
        Guid responsableId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _personas.ConsultarResponsableAsync(
                institucionId,
                responsableId,
                tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });

    [HttpGet("responsables")]
    public Task<IActionResult> BuscarResponsables(
        Guid institucionId,
        [FromQuery] BuscarResponsablesPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _personas.BuscarResponsablesAsync(
            institucionId,
            peticion,
            tokenCancelacion)));

    [HttpPost("aspirantes/{aspiranteId:guid}/responsables")]
    public Task<IActionResult> VincularResponsable(
        Guid institucionId,
        Guid aspiranteId,
        VincularResponsablePeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _personas.VincularResponsableAsync(
            institucionId,
            aspiranteId,
            peticion,
            tokenCancelacion)));

    [HttpGet("personas/historial")]
    public Task<IActionResult> ConsultarHistorial(Guid institucionId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _personas.ConsultarHistorialAsync(institucionId, tokenCancelacion)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (ConflictoVinculoException error) { return Problem(statusCode: 409, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
