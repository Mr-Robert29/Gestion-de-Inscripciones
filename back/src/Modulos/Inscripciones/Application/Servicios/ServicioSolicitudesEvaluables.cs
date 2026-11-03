using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioSolicitudesEvaluables : IModuloSolicitudesEvaluables
{
    private readonly IRepositorioSolicitudes _solicitudes;
    private readonly IAccesoInstitucionesInscripciones _acceso;

    public ServicioSolicitudesEvaluables(
        IRepositorioSolicitudes solicitudes,
        IAccesoInstitucionesInscripciones acceso)
    {
        _solicitudes = solicitudes;
        _acceso = acceso;
    }

    public async Task<IReadOnlyList<DatosSolicitudEvaluable>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(institucionId, false, tokenCancelacion);
        var solicitudes = await _solicitudes.ListarEvaluablesAsync(institucionId, periodoId, tokenCancelacion);
        return solicitudes.Select(s => new DatosSolicitudEvaluable(
            s.SolicitudId,
            s.AspiranteId,
            s.InstitucionId,
            s.PeriodoId,
            s.Sala,
            s.FueraDeTermino,
            s.Condiciones
                .Where(c => c.EstadoVerificacion == EstadoVerificacionCondicion.Verificada)
                .Select(c => c.Tipo)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            s.DistanciaMetros,
            s.RevisionDatos,
            s.RevisionDomicilio)).ToArray();
    }
}
