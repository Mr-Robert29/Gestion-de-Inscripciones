using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ofertas")]
public sealed class OfertaController : ControllerBase
{
    private readonly IModuloOferta _oferta;
    public OfertaController(IModuloOferta oferta) { _oferta = oferta; }

    [HttpPost("/api/v1/instituciones/{institucionId:guid}/aulas")]
    public Task<IActionResult> RegistrarAula(
        Guid institucionId,
        RegistrarAulaPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _oferta.RegistrarAulaAsync(
                institucionId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(ListarAulas),
                new { institucionId },
                respuesta);
        });
    }

    [HttpGet("/api/v1/instituciones/{institucionId:guid}/aulas")]
    public Task<IActionResult> ListarAulas(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _oferta.ListarAulasAsync(institucionId, tokenCancelacion)));
    }

    [HttpPost("/api/v1/instituciones/{institucionId:guid}/turnos")]
    public Task<IActionResult> RegistrarTurno(
        Guid institucionId,
        RegistrarTurnoPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _oferta.RegistrarTurnoAsync(
                institucionId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(ListarTurnos),
                new { institucionId },
                respuesta);
        });
    }

    [HttpGet("/api/v1/instituciones/{institucionId:guid}/turnos")]
    public Task<IActionResult> ListarTurnos(Guid institucionId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _oferta.ListarTurnosAsync(institucionId, tokenCancelacion)));
    }

    [HttpPost("/api/v1/periodos/{periodoId:guid}/ofertas")]
    public Task<IActionResult> Registrar(
        Guid periodoId,
        RegistrarOfertaPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _oferta.RegistrarOfertaAsync(
                periodoId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(Consultar),
                new { ofertaId = respuesta.Id },
                respuesta);
        });
    }

    [HttpGet("/api/v1/periodos/{periodoId:guid}/ofertas")]
    public Task<IActionResult> Listar(Guid periodoId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _oferta.ListarOfertasAsync(periodoId, tokenCancelacion)));
    }

    [HttpGet("{ofertaId:guid}")]
    public Task<IActionResult> Consultar(Guid ofertaId, CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () =>
        {
            var respuesta = await _oferta.ConsultarOfertaAsync(ofertaId, tokenCancelacion);
            return respuesta is null ? NotFound() : Ok(respuesta);
        });
    }

    [HttpPut("{ofertaId:guid}")]
    public Task<IActionResult> Actualizar(
        Guid ofertaId,
        ActualizarOfertaPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        return EjecutarAsync(async () => Ok(await _oferta.ActualizarOfertaAsync(
            ofertaId,
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
