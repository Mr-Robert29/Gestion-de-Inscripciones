using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/versiones-configuracion")]
public sealed class VersionesConfiguracionController : ControllerBase
{
    private readonly IModuloVersionesConfiguracion _versiones;
    public VersionesConfiguracionController(IModuloVersionesConfiguracion versiones) { _versiones = versiones; }

    [HttpPost("/api/v1/periodos/{periodoId:guid}/versiones-reglas")]
    public Task<IActionResult> PublicarReglas(
        Guid periodoId,
        PublicarReglasPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _versiones.PublicarReglasAsync(
                periodoId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(Consultar),
                new { versionId = respuesta.Id },
                respuesta);
        });
    }

    [HttpPost("/api/v1/periodos/{periodoId:guid}/versiones-oferta")]
    public Task<IActionResult> PublicarOferta(
        Guid periodoId,
        PublicarOfertaPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _versiones.PublicarOfertaAsync(
                periodoId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(Consultar),
                new { versionId = respuesta.Id },
                respuesta);
        });
    }

    [HttpGet("/api/v1/periodos/{periodoId:guid}/versiones-configuracion")]
    public Task<IActionResult> Listar(Guid periodoId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _versiones.ListarAsync(periodoId, tokenCancelacion)));

    [HttpGet("{versionId:guid}")]
    public Task<IActionResult> Consultar(Guid versionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _versiones.ConsultarAsync(versionId, tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });
    }

    [HttpPut("/api/v1/periodos/{periodoId:guid}/configuracion")]
    public Task<IActionResult> Asignar(
        Guid periodoId,
        AsignarConfiguracionPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _versiones.AsignarAsync(
            periodoId,
            peticion,
            tokenCancelacion)));

    [HttpPost("/api/v1/periodos/{periodoId:guid}/configuracion/comprobar-vigencia")]
    public Task<IActionResult> ComprobarVigencia(
        Guid periodoId,
        DatosVersionesConfiguracion versiones,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(new { Vigente = await _versiones.EstaVigenteAsync(
            periodoId,
            versiones,
            tokenCancelacion) }));

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
