using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}/solicitudes/{solicitudId:guid}")]
public sealed class AntecedentesSolicitudController : ControllerBase
{
    private readonly IModuloAntecedentesSolicitud _antecedentes;
    public AntecedentesSolicitudController(IModuloAntecedentesSolicitud antecedentes) { _antecedentes = antecedentes; }

    [HttpGet("antecedentes")]
    public Task<IActionResult> Consultar(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _antecedentes.ConsultarAsync(
            institucionId,
            solicitudId,
            tokenCancelacion)));

    [HttpPost("documentos")]
    public Task<IActionResult> RegistrarDocumento(
        Guid institucionId,
        Guid solicitudId,
        RegistrarDocumentoPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _antecedentes.RegistrarDocumentoAsync(
                institucionId,
                solicitudId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(Consultar),
                new { institucionId, solicitudId },
                respuesta);
        });

    [HttpPut("documentos/{documentoId:guid}")]
    public Task<IActionResult> RevisarDocumento(
        Guid institucionId,
        Guid solicitudId,
        Guid documentoId,
        RevisarDocumentoPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _antecedentes.RevisarDocumentoAsync(
            institucionId,
            solicitudId,
            documentoId,
            peticion,
            tokenCancelacion)));

    [HttpPost("condiciones")]
    public Task<IActionResult> RegistrarCondicion(
        Guid institucionId,
        Guid solicitudId,
        RegistrarCondicionPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _antecedentes.RegistrarCondicionAsync(
                institucionId,
                solicitudId,
                peticion,
                tokenCancelacion);
            return CreatedAtAction(
                nameof(Consultar),
                new { institucionId, solicitudId },
                respuesta);
        });

    [HttpPut("condiciones/{condicionId:guid}")]
    public Task<IActionResult> RevisarCondicion(
        Guid institucionId,
        Guid solicitudId,
        Guid condicionId,
        RevisarCondicionPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _antecedentes.RevisarCondicionAsync(
            institucionId,
            solicitudId,
            condicionId,
            peticion,
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
