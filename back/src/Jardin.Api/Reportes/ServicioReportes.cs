using Jardin.Inscripciones.Contracts;
using Jardin.Secciones.Contracts;
using Jardin.Vacantes.Contracts;

namespace Jardin.Api.Reportes;

public sealed class ServicioReportes : IConsultaReportes
{
    private readonly IAutorizacionReportes _autorizacion;
    private readonly IModuloSolicitudes _solicitudes;
    private readonly IModuloPersonasInscripciones _personas;
    private readonly IModuloPropuestasIngresos _propuestas;
    private readonly IModuloVacantes _vacantes;
    private readonly IModuloListaEspera _listaEspera;
    private readonly IModuloOfrecimientos _ofrecimientos;
    private readonly IModuloCasosRevision _casos;
    private readonly IModuloSecciones _secciones;
    private readonly IModuloComposicionSecciones _composiciones;

    public ServicioReportes(
        IAutorizacionReportes autorizacion,
        IModuloSolicitudes solicitudes,
        IModuloPersonasInscripciones personas,
        IModuloPropuestasIngresos propuestas,
        IModuloVacantes vacantes,
        IModuloListaEspera listaEspera,
        IModuloOfrecimientos ofrecimientos,
        IModuloCasosRevision casos,
        IModuloSecciones secciones,
        IModuloComposicionSecciones composiciones)
    {
        _autorizacion = autorizacion;
        _solicitudes = solicitudes;
        _personas = personas;
        _propuestas = propuestas;
        _vacantes = vacantes;
        _listaEspera = listaEspera;
        _ofrecimientos = ofrecimientos;
        _casos = casos;
        _secciones = secciones;
        _composiciones = composiciones;
    }

    public async Task<ConsultarReportesRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default)
    {
        if (institucionId == Guid.Empty) throw new ArgumentException("La institución no puede estar vacía.", nameof(institucionId));
        if (periodoId == Guid.Empty) throw new ArgumentException("El período no puede estar vacío.", nameof(periodoId));

        var acceso = await _autorizacion.AutorizarConsultaReportesAsync(periodoId, tokenCancelacion);
        if (acceso.InstitucionId != institucionId)
            throw new KeyNotFoundException("El período no pertenece a la institución indicada.");

        var solicitudes = await ListarSolicitudesAsync(institucionId, periodoId, tokenCancelacion);
        var propuestas = await _propuestas.ListarAsync(periodoId, tokenCancelacion);
        var ingresos = await _vacantes.ListarIngresosAsync(periodoId, tokenCancelacion);
        var lista = await ListarEsperaCompletaAsync(institucionId, periodoId, tokenCancelacion);
        var ofrecimientos = await _ofrecimientos.ListarAsync(periodoId, tokenCancelacion);
        var casos = await _casos.ListarParaReportesAsync(periodoId, tokenCancelacion);
        var historialCasos = new List<DatosHistorialCasoRevision>();
        foreach (var caso in casos)
            historialCasos.AddRange(await _casos.ConsultarHistorialParaReportesAsync(caso.Id, tokenCancelacion));
        historialCasos = historialCasos.OrderBy(x => x.Fecha).ToList();
        var secciones = await _secciones.ListarSeccionesPorPeriodoAsync(periodoId, tokenCancelacion);
        var distribuciones = await _secciones.ListarPropuestasDistribucionAsync(periodoId, tokenCancelacion);
        var cambiosComposicion = new List<DatosCambioComposicion>();
        foreach (var seccion in secciones)
            cambiosComposicion.AddRange(await _composiciones.ConsultarHistorialAsync(periodoId, seccion.Id, tokenCancelacion));
        cambiosComposicion = cambiosComposicion.OrderBy(x => x.Fecha).ToList();
        var idsSolicitudes = solicitudes.Select(x => x.Id).ToHashSet();
        var historialInscripciones = (await _personas.ConsultarHistorialAsync(institucionId, tokenCancelacion))
            .Where(x => idsSolicitudes.Contains(x.EntidadId))
            .OrderBy(x => x.Fecha)
            .ToArray();

        return new ConsultarReportesRespuesta(
            institucionId,
            periodoId,
            DateTimeOffset.UtcNow,
            solicitudes,
            propuestas,
            ingresos,
            lista,
            ofrecimientos,
            secciones,
            distribuciones,
            casos,
            historialCasos,
            cambiosComposicion,
            historialInscripciones);
    }

    private async Task<IReadOnlyList<ConsultarSolicitudRespuesta>> ListarSolicitudesAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        var resultados = new List<ConsultarSolicitudRespuesta>();
        for (var pagina = 1; ; pagina++)
        {
            var respuesta = await _solicitudes.ListarAsync(
                institucionId,
                periodoId,
                new ListarSolicitudesPeticion(pagina, 100),
                tokenCancelacion);
            resultados.AddRange(respuesta.Resultados);
            if (!respuesta.HayMas) return resultados;
        }
    }

    private async Task<ConsultarListaEsperaRespuesta> ListarEsperaCompletaAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion)
    {
        var enEspera = new List<DatosIntegranteListaEspera>();
        var pendientes = new List<DatosIntegranteListaEspera>();
        ConsultarListaEsperaRespuesta? primera = null;
        for (var pagina = 1; ; pagina++)
        {
            var respuesta = await _listaEspera.ConsultarAsync(
                institucionId,
                periodoId,
                new ConsultarListaEsperaPeticion(Pagina: pagina, TamanoPagina: 100),
                tokenCancelacion);
            primera ??= respuesta;
            enEspera.AddRange(respuesta.EnEspera.Resultados);
            pendientes.AddRange(respuesta.Pendientes.Resultados);
            if (!respuesta.EnEspera.HayMas && !respuesta.Pendientes.HayMas)
            {
                return respuesta with
                {
                    TotalEnEspera = enEspera.Count,
                    TotalPendientes = pendientes.Count,
                    EnEspera = respuesta.EnEspera with { Resultados = enEspera, TotalFiltrado = enEspera.Count, Pagina = 1, TamanoPagina = enEspera.Count == 0 ? 1 : enEspera.Count, HayMas = false },
                    Pendientes = respuesta.Pendientes with { Resultados = pendientes, TotalFiltrado = pendientes.Count, Pagina = 1, TamanoPagina = pendientes.Count == 0 ? 1 : pendientes.Count, HayMas = false }
                };
            }
        }
    }
}
