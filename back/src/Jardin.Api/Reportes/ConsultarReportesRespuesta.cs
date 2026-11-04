using Jardin.Inscripciones.Contracts;
using Jardin.Secciones.Contracts;
using Jardin.Vacantes.Contracts;

namespace Jardin.Api.Reportes;

public sealed record ConsultarReportesRespuesta(
    Guid InstitucionId,
    Guid PeriodoId,
    DateTimeOffset FechaConsulta,
    IReadOnlyList<ConsultarSolicitudRespuesta> Solicitudes,
    IReadOnlyList<ConsultarPropuestaIngresosRespuesta> PropuestasIngresos,
    IReadOnlyList<DatosIngresoHistorial> Ingresos,
    ConsultarListaEsperaRespuesta ListaEspera,
    IReadOnlyList<ConsultarOfrecimientoRespuesta> Ofrecimientos,
    IReadOnlyList<DatosSeccion> Secciones,
    IReadOnlyList<DatosPropuestaDistribucion> PropuestasDistribucion,
    IReadOnlyList<DatosCasoRevision> CasosRevision,
    IReadOnlyList<DatosHistorialCasoRevision> HistorialCasosRevision,
    IReadOnlyList<DatosCambioComposicion> CambiosComposicion,
    IReadOnlyList<DatosCambioInscripciones> HistorialInscripciones);
