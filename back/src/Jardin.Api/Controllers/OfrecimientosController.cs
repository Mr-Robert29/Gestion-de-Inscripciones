using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class OfrecimientosController : ControllerBase
{
    private readonly IModuloOfrecimientos _ofrecimientos;
    private readonly IModuloBajasIngresos _bajas;
    public OfrecimientosController(IModuloOfrecimientos ofrecimientos, IModuloBajasIngresos bajas)
    {
        _ofrecimientos = ofrecimientos;
        _bajas = bajas;
    }

    [HttpGet("api/v1/ingresos/{ingresoId:guid}/baja")]
    public Task<IActionResult> ConsultarBaja(Guid ingresoId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var baja = await _bajas.ConsultarAsync(ingresoId, tokenCancelacion);
            return baja is null ? NotFound() : Ok(baja);
        });

    [HttpPost("api/v1/ingresos/{ingresoId:guid}/baja")]
    public Task<IActionResult> DarBaja(
        Guid ingresoId,
        RegistrarBajaIngresoPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var baja = await _bajas.RegistrarAsync(ingresoId, peticion, tokenCancelacion);
            return baja.PlazaLiberada ? Ok(baja) : StatusCode(202, baja);
        });

    [HttpGet("api/v1/periodos/{periodoId:guid}/ofertas/{ofertaId:guid}/candidatos")]
    public Task<IActionResult> Candidatos(
        Guid periodoId,
        Guid ofertaId,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _ofrecimientos.ConsultarCandidatosAsync(periodoId, ofertaId, tokenCancelacion)));

    [HttpPost("api/v1/periodos/{periodoId:guid}/ofrecimientos")]
    public Task<IActionResult> Ofrecer(
        Guid periodoId,
        OfrecerVacantePeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var resultado = await _ofrecimientos.OfrecerAsync(periodoId, peticion, tokenCancelacion);
            return Created($"/api/v1/ofrecimientos/{resultado.Id}", resultado);
        });

    [HttpGet("api/v1/periodos/{periodoId:guid}/ofrecimientos")]
    public Task<IActionResult> Listar(Guid periodoId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _ofrecimientos.ListarAsync(periodoId, tokenCancelacion)));

    [HttpGet("api/v1/ofrecimientos/{ofrecimientoId:guid}")]
    public Task<IActionResult> Consultar(Guid ofrecimientoId, CancellationToken tokenCancelacion)
        => EjecutarAsync(async () =>
        {
            var resultado = await _ofrecimientos.ConsultarAsync(ofrecimientoId, tokenCancelacion);
            return resultado is null ? NotFound() : Ok(resultado);
        });

    [HttpPost("api/v1/ofrecimientos/{ofrecimientoId:guid}/respuestas")]
    public Task<IActionResult> Responder(
        Guid ofrecimientoId,
        ResponderOfrecimientoPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _ofrecimientos.ResponderAsync(ofrecimientoId, peticion, tokenCancelacion)));

    [HttpPost("api/v1/ofrecimientos/{ofrecimientoId:guid}/confirmacion")]
    public Task<IActionResult> Confirmar(
        Guid ofrecimientoId,
        ConfirmarOfrecimientoPeticion peticion,
        CancellationToken tokenCancelacion)
        => EjecutarAsync(async () => Ok(await _ofrecimientos.ConfirmarAsync(ofrecimientoId, peticion, tokenCancelacion)));

    private static async Task<IActionResult> EjecutarAsync(Func<Task<IActionResult>> operacion)
    {
        try { return await operacion(); }
        catch (UnauthorizedAccessException) { return new ForbidResult(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }
    private static ObjectResult Error(int estado, string mensaje)
        => new(new ProblemDetails { Status = estado, Detail = mensaje }) { StatusCode = estado };
}
