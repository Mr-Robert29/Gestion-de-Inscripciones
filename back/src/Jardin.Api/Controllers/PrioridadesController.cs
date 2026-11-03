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
    public Task<IActionResult> Evaluar(
        Guid periodoId,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _prioridades.EvaluarAsync(periodoId, cancellationToken)));

    private async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Problem(statusCode: 404, detail: error.Message); }
        catch (ArgumentException error) { return Problem(statusCode: 400, detail: error.Message); }
        catch (InvalidOperationException error) { return Problem(statusCode: 409, detail: error.Message); }
    }
}
