using System.Security.Cryptography;
using System.Text;
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
    private readonly IModuloAntecedentesSolicitud _antecedentes;
    private readonly IRepositorioEvaluacionesPrioridad _evaluaciones;

    public ServicioPrioridades(
        IAutorizacionVacantes autorizacion,
        IProveedorEstadoPeriodo periodos,
        IProveedorReglasPeriodo reglas,
        IModuloSolicitudesEvaluables solicitudes,
        IModuloAntecedentesSolicitud antecedentes,
        IRepositorioEvaluacionesPrioridad evaluaciones)
    {
        _autorizacion = autorizacion;
        _periodos = periodos;
        _reglas = reglas;
        _solicitudes = solicitudes;
        _antecedentes = antecedentes;
        _evaluaciones = evaluaciones;
    }

    public async Task<DatosEstadoProcesamientoPrioridades> ConsultarEstadoAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        var estado = await _periodos.ConsultarAsync(periodoId, cancellationToken);
        var condiciones = await _antecedentes.ListarArticulacionesAsync(
            autorizacion.InstitucionId,
            periodoId,
            cancellationToken);
        var pendientes = ContarPendientes(condiciones);
        if (!estado.RecepcionCerrada)
            return new DatosEstadoProcesamientoPrioridades(false, false, false, null, 0, pendientes, []);
        var ultima = (await _evaluaciones.ListarAsync(periodoId, cancellationToken)).FirstOrDefault();
        if (ultima is null)
            return new DatosEstadoProcesamientoPrioridades(
                estado.RecepcionCerrada, false, false, null, 0, pendientes, []);

        var guardada = Deserializar(ultima.Datos);
        var preparada = await PrepararAsync(periodoId, autorizacion.InstitucionId, cancellationToken);
        var vigente = pendientes == 0 &&
            guardada.HuellaEntradas is not null &&
            string.Equals(guardada.HuellaEntradas, preparada.Huella, StringComparison.Ordinal);
        return new DatosEstadoProcesamientoPrioridades(
            estado.RecepcionCerrada,
            true,
            vigente,
            ultima.Fecha,
            guardada.Resultados.Count,
            pendientes,
            guardada.Resultados);
    }

    public async Task<IReadOnlyList<DatosPrioridad>> ProcesarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        var preparada = await PrepararAsync(periodoId, autorizacion.InstitucionId, cancellationToken);
        if (!preparada.RecepcionCerrada)
            throw new InvalidOperationException("Cerrá la recepción antes de procesar la lista de aspirantes.");
        if (preparada.PendientesRevision > 0)
            throw new InvalidOperationException($"Quedan {preparada.PendientesRevision} prioridades por revisar. Resolvé esos casos antes de procesar la lista.");
        var respuesta = Evaluar(preparada.Solicitudes, preparada.Reglas);

        // Volvemos a leer las entradas para no guardar un procesamiento si los datos cambiaron durante el cálculo.
        var verificacion = await PrepararAsync(periodoId, autorizacion.InstitucionId, cancellationToken);
        if (!verificacion.RecepcionCerrada || verificacion.PendientesRevision > 0 ||
            !string.Equals(preparada.Huella, verificacion.Huella, StringComparison.Ordinal))
            throw new InvalidOperationException("Los datos cambiaron durante el procesamiento. Actualizá la pantalla e intentá nuevamente.");

        await _evaluaciones.GuardarAsync(
            new EvaluacionPrioridad(
                periodoId,
                preparada.Reglas.VersionReglasId,
                autorizacion.UsuarioId,
                preparada.RevisionPeriodo,
                JsonSerializer.Serialize(new InstantaneaProcesada(preparada.Huella, respuesta))),
            cancellationToken);
        return respuesta;
    }

    // Se conserva como cálculo transitorio para los servicios internos que detectan empates y casos manuales.
    public async Task<IReadOnlyList<DatosPrioridad>> EvaluarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await _autorizacion.AutorizarAsync(periodoId, cancellationToken);
        var preparada = await PrepararAsync(periodoId, autorizacion.InstitucionId, cancellationToken);
        return Evaluar(preparada.Solicitudes, preparada.Reglas);
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
            Deserializar(x.Datos).Resultados))
            .ToArray();
    }

    private async Task<DatosPreparacion> PrepararAsync(
        Guid periodoId,
        Guid institucionId,
        CancellationToken cancellationToken)
    {
        var estado = await _periodos.ConsultarAsync(periodoId, cancellationToken);
        var reglas = await _reglas.ConsultarAsync(periodoId, cancellationToken);
        var solicitudes = (await _solicitudes.ListarAsync(institucionId, periodoId, cancellationToken))
            .OrderBy(x => x.SolicitudId)
            .ToArray();
        var declaraciones = await _antecedentes.ListarArticulacionesAsync(institucionId, periodoId, cancellationToken);
        var condiciones = declaraciones
            .OrderBy(x => x.Condicion.Id)
            .Select(x => new
            {
                x.SolicitudId,
                x.RevisionSolicitud,
                x.Condicion.Id,
                x.Condicion.Tipo,
                x.Condicion.ValorDeclarado,
                x.Condicion.EstadoVerificacion,
                x.Condicion.EvidenciaReferencia,
                x.Condicion.Observacion
            })
            .ToArray();
        var huella = CalcularHuella(new
        {
            estado.RecepcionCerrada,
            estado.Revision,
            reglas.VersionReglasId,
            reglas.OrdenSalas,
            reglas.OrdenProcedimiento,
            reglas.PrecisionDistanciaDecimales,
            Solicitudes = solicitudes,
            Condiciones = condiciones
        });
        return new DatosPreparacion(reglas, solicitudes, huella, estado.RecepcionCerrada, estado.Revision, ContarPendientes(declaraciones));
    }

    private static IReadOnlyList<DatosPrioridad> Evaluar(
        IReadOnlyList<DatosSolicitudEvaluable> solicitudes,
        ReglasPeriodoVacantes reglas)
    {
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
        return resultados.Select(x => new DatosPrioridad(
            x.SolicitudId,
            x.AspiranteId,
            x.Sala,
            x.Posicion,
            x.RequiereRevision,
            x.MotivoRevision,
            x.CriteriosAplicados,
            x.Explicacion,
            reglas.VersionReglasId)).ToArray();
    }

    private static int ContarPendientes(IEnumerable<DatosArticulacionSolicitud> condiciones)
        => condiciones.Count(x => x.Condicion.EstadoVerificacion is "PendienteRevision" or "Observada");

    private static string CalcularHuella(object datos)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datos))));

    private static InstantaneaProcesada Deserializar(string datos)
    {
        // Las evaluaciones anteriores a este flujo guardaban solo el arreglo y no cuentan como procesamiento vigente.
        if (datos.AsSpan().TrimStart().StartsWith("[", StringComparison.Ordinal))
            return new InstantaneaProcesada(null, JsonSerializer.Deserialize<DatosPrioridad[]>(datos) ?? []);
        return JsonSerializer.Deserialize<InstantaneaProcesada>(datos)
            ?? new InstantaneaProcesada(null, []);
    }

    private sealed record DatosPreparacion(
        ReglasPeriodoVacantes Reglas,
        IReadOnlyList<DatosSolicitudEvaluable> Solicitudes,
        string Huella,
        bool RecepcionCerrada,
        int RevisionPeriodo,
        int PendientesRevision);

    private sealed record InstantaneaProcesada(
        string? HuellaEntradas,
        IReadOnlyList<DatosPrioridad> Resultados);
}
