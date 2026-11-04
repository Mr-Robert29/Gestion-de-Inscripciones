using System.Text.Json;
using Jardin.Inscripciones.Contracts;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioPrioridades : IModuloPrioridades
{
    private readonly IAutorizacionVacantes _autorizacion;
    private readonly IProveedorEstadoPeriodo _periodos;
    private readonly IProveedorReglasPeriodo _reglas;
    private readonly IModuloSolicitudesEvaluables _solicitudes;
    private readonly IRepositorioEvaluacionesPrioridad _evaluaciones;

    public ServicioPrioridades(
        IAutorizacionVacantes autorizacion,
        IProveedorEstadoPeriodo periodos,
        IProveedorReglasPeriodo reglas,
        IModuloSolicitudesEvaluables solicitudes,
        IRepositorioEvaluacionesPrioridad evaluaciones)
    {
        _autorizacion = autorizacion;
        _periodos = periodos;
        _reglas = reglas;
        _solicitudes = solicitudes;
        _evaluaciones = evaluaciones;
    }

    public async Task<IReadOnlyList<DatosPrioridad>> EvaluarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        var estado = await _periodos.ConsultarAsync(periodoId, cancellationToken);
        if (!estado.RecepcionCerrada)
            throw new InvalidOperationException("Las prioridades solo pueden evaluarse con la recepción cerrada.");
        var reglas = await _reglas.ConsultarAsync(periodoId, cancellationToken);
        var solicitudes = await _solicitudes.ListarAsync(autorizacion.InstitucionId, periodoId, cancellationToken);
        var resultados = EvaluadorPrioridades.Evaluar(
            solicitudes.Select(x => new EntradaEvaluacionPrioridad(
                x.SolicitudId,
                x.AspiranteId,
                x.Sala,
                x.FueraDeTermino,
                x.CondicionesVerificadas.ToDictionary(c => c, _ => true, StringComparer.Ordinal),
                x.DistanciaMetros)),
            reglas.OrdenSalas,
            reglas.OrdenProcedimiento,
            reglas.PrecisionDistanciaDecimales);
        var respuesta = resultados.Select(x => new DatosPrioridad(
            x.SolicitudId,
            x.AspiranteId,
            x.Sala,
            x.Posicion,
            x.RequiereRevision,
            x.MotivoRevision,
            x.CriteriosAplicados,
            x.Explicacion,
            reglas.VersionReglasId)).ToArray();
        await _evaluaciones.GuardarAsync(
            new EvaluacionPrioridad(
                periodoId,
                reglas.VersionReglasId,
                autorizacion.UsuarioId,
                estado.Revision,
                JsonSerializer.Serialize(respuesta)),
            cancellationToken);
        return respuesta;
    }

    public async Task<IReadOnlyList<DatosEvaluacionPrioridad>> ConsultarHistorialAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        var evaluaciones = await _evaluaciones.ListarAsync(periodoId, cancellationToken);
        return evaluaciones.Select(x => new DatosEvaluacionPrioridad(
            x.Id,
            x.PeriodoId,
            x.VersionReglasId,
            x.UsuarioId,
            x.RevisionPeriodo,
            x.Fecha,
            JsonSerializer.Deserialize<DatosPrioridad[]>(x.Datos) ?? []))
            .ToArray();
    }
}
