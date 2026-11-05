using Jardin.Api.Seguridad;
using Jardin.Secciones.Contracts;
using Jardin.Usuarios.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Route("api/v1/periodos/{periodoId:guid}/secciones")]
[Authorize]
[TypeFilter(typeof(FiltroErroresSecciones))]
public sealed class SeccionesController : ControllerBase
{
    private readonly IModuloSecciones _modulo;

    public SeccionesController(IModuloSecciones modulo)
    {
        _modulo = modulo;
    }

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Definir(
        Guid periodoId,
        [FromBody] DefinirSeccionPeticion peticion,
        CancellationToken cancellationToken)
    {
        if (periodoId != peticion.PeriodoId)
            return BadRequest(new { mensaje = "El período de la ruta no coincide con el período de la petición." });

        try
        {
            var id = await _modulo.DefinirSeccionAsync(peticion, cancellationToken);
            return CreatedAtAction(nameof(Obtener), new { periodoId, seccionId = id }, new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DatosSeccion>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(Guid periodoId, CancellationToken cancellationToken)
    {
        var resultado = await _modulo.ListarSeccionesPorPeriodoAsync(periodoId, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{seccionId:guid}")]
    [ProducesResponseType(typeof(DatosSeccion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(
        Guid periodoId,
        Guid seccionId,
        CancellationToken cancellationToken)
    {
        var resultado = await _modulo.ObtenerSeccionPorIdAsync(seccionId, cancellationToken);
        if (resultado is null || resultado.PeriodoId != periodoId)
            return NotFound();

        return Ok(resultado);
    }
}
