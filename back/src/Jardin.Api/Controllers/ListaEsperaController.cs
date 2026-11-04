using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ListaEsperaController : ControllerBase
{
    private readonly IModuloListaEspera _lista;

    public ListaEsperaController(IModuloListaEspera lista) { _lista = lista; }

    [HttpGet("api/v1/instituciones/{institucionId:guid}/periodos/{periodoId:guid}/lista-espera")]
    public async Task<IActionResult> Consultar(
        Guid institucionId,
        Guid periodoId,
        [FromQuery] ConsultarListaEsperaPeticion peticion,
        CancellationToken tokenCancelacion)
    {
        try
        {
            return Ok(await _lista.ConsultarAsync(
                institucionId,
                periodoId,
                peticion,
                tokenCancelacion));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    private static ObjectResult Error(int estado, string mensaje)
        => new(new ProblemDetails { Status = estado, Detail = mensaje }) { StatusCode = estado };
}
