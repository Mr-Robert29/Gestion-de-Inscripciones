using System.Globalization;
using System.Text;
using Jardin.Api.Reportes;
using Jardin.Inscripciones.Contracts;
using Jardin.Secciones.Contracts;
using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jardin.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ReportesController : ControllerBase
{
    private readonly IConsultaReportes _reportes;

    public ReportesController(IConsultaReportes reportes)
    {
        _reportes = reportes;
    }

    [HttpGet("api/v1/instituciones/{institucionId:guid}/periodos/{periodoId:guid}/reportes")]
    public async Task<IActionResult> Consultar(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        try
        {
            return Ok(await _reportes.ConsultarAsync(institucionId, periodoId, tokenCancelacion));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    [HttpGet("api/v1/instituciones/{institucionId:guid}/periodos/{periodoId:guid}/reportes/exportar")]
    [Produces("text/csv")]
    public async Task<IActionResult> Exportar(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        try
        {
            var reporte = await _reportes.ConsultarAsync(institucionId, periodoId, tokenCancelacion);
            var contenido = GenerarCsv(reporte);
            return File(
                Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(contenido)).ToArray(),
                "text/csv; charset=utf-8",
                $"reporte-periodo-{periodoId:N}.csv");
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException error) { return Error(404, error.Message); }
        catch (ArgumentException error) { return Error(400, error.Message); }
        catch (InvalidOperationException error) { return Error(409, error.Message); }
    }

    private static string GenerarCsv(ConsultarReportesRespuesta reporte)
    {
        var salida = new StringBuilder();
        salida.AppendLine("Tipo;PeriodoId;InstitucionId;Id;RelacionadoId;Estado;UsuarioId;Fecha;Motivo;Datos");

        foreach (var solicitud in reporte.Solicitudes)
            Fila(salida, "Solicitud", reporte, solicitud.Id, solicitud.AspiranteId, solicitud.EstadoRecepcion, solicitud.UsuarioRegistroId, solicitud.FechaRegistro, null, $"Edad={solicitud.EdadCalculada};Sala={solicitud.Sala}");
        foreach (var propuesta in reporte.PropuestasIngresos)
            Fila(salida, "PropuestaIngreso", reporte, propuesta.Id, propuesta.PeriodoId, propuesta.Estado, propuesta.UsuarioId, propuesta.Fecha, propuesta.Motivo, $"Revision={propuesta.Revision};Resultados={propuesta.Resultados.Count}");
        foreach (var ingreso in reporte.Ingresos)
            Fila(salida, "Ingreso", reporte, ingreso.Id, ingreso.SolicitudId, ingreso.Activo ? "Activo" : "Baja", ingreso.Activo ? ingreso.UsuarioId : ingreso.UsuarioBajaId, ingreso.FechaBaja ?? ingreso.Fecha, ingreso.MotivoBaja, $"Edad={ingreso.Edad};Sala={ingreso.Sala}");
        foreach (var ofrecimiento in reporte.Ofrecimientos)
            Fila(salida, "Ofrecimiento", reporte, ofrecimiento.Id, ofrecimiento.SolicitudId, ofrecimiento.Estado, ofrecimiento.UsuarioId, ofrecimiento.Fecha, ofrecimiento.Motivo, $"OfertaId={ofrecimiento.OfertaId};Revision={ofrecimiento.Revision}");
        foreach (var seccion in reporte.Secciones)
            Fila(salida, "Seccion", reporte, seccion.Id, seccion.PeriodoId, seccion.Tipo, null, null, null, $"AulaId={seccion.AulaId};TurnoId={seccion.TurnoId};Edades={string.Join(',', seccion.EdadesAdmitidas)}");
        foreach (var distribucion in reporte.PropuestasDistribucion)
            Fila(salida, "Distribucion", reporte, distribucion.Id, distribucion.PeriodoId, distribucion.Estado, distribucion.ConfirmadaPor ?? distribucion.CreadaPor, distribucion.FechaConfirmacion ?? distribucion.FechaCreacion, null, $"Version={distribucion.Version};Asignaciones={distribucion.Asignaciones.Count}");
        foreach (var caso in reporte.CasosRevision)
            Fila(salida, "CasoRevision", reporte, caso.Id, caso.PeriodoId, caso.Estado, caso.UsuarioResolucion, caso.FechaResolucion ?? caso.FechaCreacion, caso.MotivoResolucion, $"Tipo={caso.Tipo};Sala={caso.Sala}");
        foreach (var cambio in reporte.CambiosComposicion)
            Fila(salida, "CambioComposicion", reporte, cambio.Id, cambio.SeccionId, "Registrado", cambio.UsuarioId, cambio.Fecha, cambio.Motivo, $"Antes={cambio.EdadesAnteriores};Despues={cambio.EdadesNuevas}");
        foreach (var cambio in reporte.HistorialInscripciones)
            Fila(salida, "HistorialInscripcion", reporte, cambio.Id, cambio.EntidadId, cambio.Accion, cambio.UsuarioId, cambio.Fecha, null, cambio.Datos);

        return salida.ToString();
    }

    private static void Fila(
        StringBuilder salida,
        string tipo,
        ConsultarReportesRespuesta reporte,
        Guid id,
        Guid? relacionadoId,
        string? estado,
        Guid? usuarioId,
        DateTimeOffset? fecha,
        string? motivo,
        string? datos)
    {
        salida.AppendLine(string.Join(';',
            Campo(tipo),
            Campo(reporte.PeriodoId),
            Campo(reporte.InstitucionId),
            Campo(id),
            Campo(relacionadoId),
            Campo(estado),
            Campo(usuarioId),
            Campo(fecha),
            Campo(motivo),
            Campo(datos)));
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
