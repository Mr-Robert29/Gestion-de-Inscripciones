using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/instituciones/{institucionId:guid}/solicitudes")]
public sealed class CorreccionesSolicitudesController : ControllerBase
{
    private readonly IModuloCorreccionesSolicitudes _correcciones;
    private readonly IModuloUsoDatosSolicitud _usos;
    public CorreccionesSolicitudesController(IModuloCorreccionesSolicitudes correcciones, IModuloUsoDatosSolicitud usos)
    {
        _correcciones = correcciones;
        _usos = usos;
    }

    [HttpPost("buscar")]
    public Task<IActionResult> Buscar(
        Guid institucionId,
        BuscarSolicitudesPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _correcciones.BuscarAsync(
            institucionId,
            peticion,
            tokenCancelacion)));

    [HttpPut("{solicitudId:guid}")]
    public Task<IActionResult> Corregir(
        Guid institucionId,
        Guid solicitudId,
        CorregirSolicitudPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _correcciones.CorregirAsync(
            institucionId,
            solicitudId,
            peticion,
            tokenCancelacion)));

    [HttpGet("{solicitudId:guid}/impacto")]
    public Task<IActionResult> ConsultarImpacto(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _usos.ConsultarImpactoAsync(
            institucionId,
            solicitudId,
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
