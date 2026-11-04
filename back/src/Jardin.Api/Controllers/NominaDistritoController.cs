using System.Globalization;
using System.Text;
using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class NominaDistritoController : ControllerBase
{
    private readonly IModuloNominaDistrito _nomina;

    public NominaDistritoController(IModuloNominaDistrito nomina)
    {
        _nomina = nomina;
    }

    [HttpGet("api/v1/instituciones/{institucionId:guid}/periodos/{periodoId:guid}/nomina-distrital")]
    public async Task<IActionResult> Consultar(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        try
        {
            return Ok(await _nomina.ConsultarAsync(institucionId, periodoId, tokenCancelacion));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    [HttpGet("api/v1/instituciones/{institucionId:guid}/periodos/{periodoId:guid}/nomina-distrital/exportar")]
    [Produces("text/csv")]
    public async Task<IActionResult> Exportar(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        try
        {
            var nomina = await _nomina.ConsultarAsync(institucionId, periodoId, tokenCancelacion);
            var contenido = GenerarCsv(nomina);
            return File(
                Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(contenido)).ToArray(),
                "text/csv; charset=utf-8",
                $"nomina-distrital-{periodoId:N}.csv");
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    private static string GenerarCsv(ConsultarNominaDistritoRespuesta nomina)
    {
        var filas = new StringBuilder();
        filas.AppendLine("Tipo;SolicitudId;AspiranteId;Nombre;Apellido;Dni;Sala;Edad;PosicionGeneral;PosicionEspera;TurnoPreferidoId;DistanciaMetros;Estado;Motivo");
        foreach (var dato in nomina.SinVacante)
            AgregarFila(filas, "SinVacante", dato);
        foreach (var dato in nomina.Pendientes)
            AgregarFila(filas, "PendienteRevision", dato);
        return filas.ToString();
    }

    private static void AgregarFila(StringBuilder salida, string tipo, DatosNominaDistrito dato)
    {
        salida.AppendLine(string.Join(';',
            Campo(tipo),
            Campo(dato.SolicitudId),
            Campo(dato.AspiranteId),
            Campo(dato.Nombre),
            Campo(dato.Apellido),
            Campo(dato.Dni),
            Campo(dato.Sala),
            Campo(dato.Edad),
            Campo(dato.PosicionGeneral),
            Campo(dato.PosicionEspera),
            Campo(dato.TurnoPreferidoId),
            Campo(dato.DistanciaMetros),
            Campo(dato.Estado),
            Campo(dato.Motivo)));
    }

    private static string Campo(object? valor)
    {
        var texto = valor switch
        {
            null => string.Empty,
            IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => valor.ToString() ?? string.Empty
        };
        return $"\"{texto.Replace("\"", "\"\"")}\"";
    }

    private static ObjectResult Error(int estado, string mensaje)
        => new(new ProblemDetails { Status = estado, Detail = mensaje }) { StatusCode = estado };
}
