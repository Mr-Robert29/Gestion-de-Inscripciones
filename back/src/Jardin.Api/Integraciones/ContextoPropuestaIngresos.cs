using System.Text.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Api.Integraciones;

// Consume contratos públicos; Vacantes no accede a tablas privadas de otros módulos.
public sealed class ContextoPropuestaIngresos : IContextoPropuestaIngresos
{
    private readonly IModuloPeriodos _periodos;
    private readonly IModuloVersionesConfiguracion _versiones;
    private readonly IModuloSolicitudesEvaluables _evaluables;
    private readonly IModuloSolicitudes _solicitudes;
    private readonly IModuloParticipantesSorteo _participantes;
    private readonly IModuloDistancias _distancias;
    private readonly IModuloPersonasInscripciones _personas;
    private readonly IModuloDuplicadosSolicitud _duplicados;

    public ContextoPropuestaIngresos(
        IModuloPeriodos periodos,
        IModuloVersionesConfiguracion versiones,
        IModuloSolicitudesEvaluables evaluables,
        IModuloSolicitudes solicitudes,
        IModuloParticipantesSorteo participantes,
        IModuloDistancias distancias,
        IModuloPersonasInscripciones personas,
        IModuloDuplicadosSolicitud duplicados)
    {
        _periodos = periodos;
        _versiones = versiones;
        _evaluables = evaluables;
        _solicitudes = solicitudes;
        _participantes = participantes;
        _distancias = distancias;
        _personas = personas;
        _duplicados = duplicados;
    }

    // Implementa IContextoPropuestaIngresos.ConPeriodoProtegidoAsync; no bloquea las escrituras de otros módulos.
    public Task<T> ConPeriodoProtegidoAsync<T>(
        Guid periodoId,
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => _periodos.EjecutarConPeriodoProtegidoAsync(
            periodoId,
            (_, token) => operacion(token),
            tokenCancelacion);

    // Implementa IContextoPropuestaIngresos.LeerAsync: conserva versiones y revisiones, sin modificar fuentes.
    public async Task<EntradasPropuestaIngresos> LeerAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        var periodo = await _periodos.ConsultarPeriodoAsync(periodoId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró el período.");
        if (periodo.VersionReglasId is null || periodo.VersionOfertaId is null)
            throw new InvalidOperationException("Asigná versiones de reglas y oferta al período.");
        var reglas = await LeerVersionAsync<DatosReglasPeriodo>(periodoId, periodo.VersionReglasId.Value, "Reglas", tokenCancelacion);
        var ofertas = await LeerVersionAsync<ConsultarOfertaRespuesta[]>(periodoId, periodo.VersionOfertaId.Value, "Oferta", tokenCancelacion);
        if (ofertas.Any(x => x.PeriodoId != periodoId) || ofertas.Select(x => (x.AulaId, x.TurnoId)).Distinct().Count() != ofertas.Length)
            throw new InvalidOperationException("La oferta publicada contiene recursos incompatibles o repetidos.");
        var evaluables = await _evaluables.ListarAsync(periodo.InstitucionId, periodoId, tokenCancelacion);
        var dni = new Dictionary<Guid, string?>();
        foreach (var sala in new[] { 5, 4, 3 })
        {
            foreach (var participante in await _participantes.ListarAsync(periodo.InstitucionId, periodoId, sala, tokenCancelacion))
                dni[participante.SolicitudId] = participante.Dni;
        }
        var cambios = await _personas.ConsultarHistorialAsync(periodo.InstitucionId, tokenCancelacion);
        var duplicaciones = new HashSet<Guid>();
        for (var pagina = 1; ; pagina++)
        {
            var alertas = await _duplicados.ListarAsync(
                periodo.InstitucionId,
                periodoId,
                new ListarDuplicadosPeticion(Pagina: pagina, TamanoPagina: 100),
                tokenCancelacion);
            foreach (var alerta in alertas.Resultados.Where(x => x.Estado != "PersonasDiferentes"))
            {
                duplicaciones.Add(alerta.SolicitudAId);
                duplicaciones.Add(alerta.SolicitudBId);
            }
            if (!alertas.HayMas) break;
        }
        var solicitudes = new List<SolicitudPropuesta>();
        foreach (var evaluable in evaluables.OrderBy(x => x.SolicitudId))
        {
            var solicitud = await _solicitudes.ConsultarAsync(periodo.InstitucionId, evaluable.SolicitudId, tokenCancelacion)
                ?? throw new InvalidOperationException("Una solicitud cambió durante la consulta.");
            var medicion = (await _distancias.ConsultarHistorialAsync(periodo.InstitucionId, solicitud.Id, tokenCancelacion))
                .FirstOrDefault(x => x.EsVigente);
            var ultimoCambio = cambios.Where(x => x.EntidadId == solicitud.Id).Select(x => x.Fecha)
                .Append(solicitud.FechaRegistro).Append(medicion?.FechaRegistro ?? DateTimeOffset.MinValue).Max();
            if (solicitud.RevisionDatos != evaluable.RevisionDatos || solicitud.RevisionDomicilio != evaluable.RevisionDomicilio)
                throw new InvalidOperationException("La solicitud cambió durante la consulta. Generá nuevamente.");
            solicitudes.Add(new SolicitudPropuesta(
                solicitud.Id,
                solicitud.AspiranteId,
                solicitud.Sala,
                solicitud.FueraDeTermino,
                evaluable.CondicionesVerificadas.Order(StringComparer.Ordinal).ToArray(),
                medicion?.DistanciaMetros,
                solicitud.RevisionDatos,
                solicitud.RevisionDomicilio,
                solicitud.TurnoPreferidoId,
                solicitud.TurnosAlternativosAceptados.ToArray(),
                dni.GetValueOrDefault(solicitud.Id),
                medicion?.Id,
                ultimoCambio,
                duplicaciones.Contains(solicitud.Id)));
        }
        return new EntradasPropuestaIngresos(
            periodo.InstitucionId,
            periodoId,
            periodo.Revision,
            periodo.Estado,
            periodo.VersionReglasId.Value,
            periodo.VersionOfertaId.Value,
            reglas.OrdenGruposEtarios,
            reglas.OrdenProcedimiento,
            reglas.CombinacionesEdades,
            ofertas.OrderBy(x => x.Id).Select(x => new OfertaPropuesta(
                x.Id,
                x.AulaId,
                x.TurnoId,
                x.Capacidad,
                x.Disponible,
                x.Ocupaciones.Where(o => o.Cantidad > 0).Select(o => o.Sala).Distinct().ToArray(),
                checked((int)x.Ocupaciones.Sum(o => (long)o.Cantidad)))).ToArray(),
            solicitudes,
            reglas.PrecisionDistanciaDecimales);
    }

    private async Task<T> LeerVersionAsync<T>(
        Guid periodoId,
        Guid versionId,
        string tipo,
        CancellationToken tokenCancelacion)
    {
        var version = await _versiones.ConsultarAsync(versionId, tokenCancelacion)
            ?? throw new InvalidOperationException("No se encontró una versión asignada.");
        if (version.PeriodoId != periodoId || version.Tipo != tipo || version.Formato != 1)
            throw new InvalidOperationException("La versión asignada no es compatible.");
        return version.Datos.Deserialize<T>() ?? throw new InvalidOperationException("La versión está vacía.");
    }
}
