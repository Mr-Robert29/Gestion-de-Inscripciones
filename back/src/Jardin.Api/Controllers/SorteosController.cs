using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/periodos")]
public sealed class SorteosController : ControllerBase
{
    private readonly IModuloSorteos _sorteos;

    public SorteosController(IModuloSorteos sorteos)
    {
        _sorteos = sorteos;
    }

    [HttpPost("{periodoId:guid}/sorteos")]
    public Task<IActionResult> Registrar(
        Guid periodoId,
        RegistrarSorteoPeticion peticion,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () =>
        {
            var respuesta = await _sorteos.RegistrarAsync(periodoId, peticion, cancellationToken);
            return CreatedAtAction(nameof(Listar), new { periodoId }, respuesta);
        });

    [HttpGet("{periodoId:guid}/sorteos")]
    public Task<IActionResult> Listar(
        Guid periodoId,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _sorteos.ListarVigentesAsync(periodoId, cancellationToken)));

    [HttpGet("{periodoId:guid}/sorteos/{sala:int}/orden")]
    public Task<IActionResult> CalcularOrden(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _sorteos.CalcularOrdenAsync(periodoId, sala, cancellationToken)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
