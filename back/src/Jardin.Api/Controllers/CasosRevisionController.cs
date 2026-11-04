using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class CasosRevisionController : ControllerBase
{
    private readonly IModuloCasosRevision _casos;

    public CasosRevisionController(IModuloCasosRevision casos)
    {
        _casos = casos;
    }

    [HttpGet("api/v1/periodos/{periodoId:guid}/casos-revision")]
    public Task<IActionResult> Listar(Guid periodoId, CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _casos.ListarAsync(periodoId, cancellationToken)));

    [HttpGet("api/v1/casos-revision/{casoId:guid}")]
    public Task<IActionResult> Consultar(Guid casoId, CancellationToken cancellationToken)
        => EjecutarAsync(async () =>
        {
            var caso = await _casos.ConsultarAsync(casoId, cancellationToken);
            return caso is null ? NotFound() : Ok(caso);
        });

    [HttpPost("api/v1/casos-revision/{casoId:guid}/resolver")]
    public Task<IActionResult> Resolver(
        Guid casoId,
        ResolverCasoRevisionPeticion peticion,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _casos.ResolverAsync(casoId, peticion, cancellationToken)));

    [HttpGet("api/v1/casos-revision/{casoId:guid}/historial")]
    public Task<IActionResult> Historial(Guid casoId, CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _casos.ConsultarHistorialAsync(casoId, cancellationToken)));

    [HttpPost("api/v1/casos-revision/{casoId:guid}/reabrir")]
    public Task<IActionResult> Reabrir(
        Guid casoId,
        ReabrirCasoRevisionPeticion peticion,
        CancellationToken cancellationToken)
        => EjecutarAsync(async () => Ok(await _casos.ReabrirAsync(casoId, peticion, cancellationToken)));

    private static async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return new ForbidResult(); }
        catch (KeyNotFoundException error) { return new ObjectResult(new ProblemDetails { Status = 404, Detail = error.Message }) { StatusCode = 404 }; }
        catch (ArgumentException error) { return new ObjectResult(new ProblemDetails { Status = 400, Detail = error.Message }) { StatusCode = 400 }; }
        catch (InvalidOperationException error) { return new ObjectResult(new ProblemDetails { Status = 409, Detail = error.Message }) { StatusCode = 409 }; }
    }
}
