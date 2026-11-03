using System.Text.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Application.Configuracion;

namespace Jardin.Api.Integraciones;

// Integra contratos públicos: Inscripciones no accede a tablas ni entidades de Configuración.
public sealed class EjecucionPeriodoSolicitud : IEjecucionPeriodoSolicitud
{
    private readonly IModuloPeriodos _periodos;
    private readonly IModuloVersionesConfiguracion _versiones;
    public EjecucionPeriodoSolicitud(IModuloPeriodos periodos, IModuloVersionesConfiguracion versiones)
    {
        _periodos = periodos;
        _versiones = versiones;
    }

    // Implementa IEjecucionPeriodoSolicitud.EjecutarAsync: conserva el bloqueo hasta terminar la operación.
    public Task<T> EjecutarAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Func<DatosPeriodoSolicitud, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => _periodos.EjecutarConPeriodoProtegidoAsync(
            periodoId,
            async (periodo, token) =>
            {
                if (periodo.InstitucionId != institucionId) throw new KeyNotFoundException("El período no pertenece a esta institución.");
                if (periodo.VersionReglasId is null || periodo.VersionOfertaId is null)
                    throw new InvalidOperationException("El período necesita versiones de reglas y oferta asignadas para registrar solicitudes.");
                var reglas = await _versiones.ConsultarAsync(periodo.VersionReglasId.Value, token);
                var oferta = await _versiones.ConsultarAsync(periodo.VersionOfertaId.Value, token);
                if (reglas is null || oferta is null || reglas.PeriodoId != periodo.Id || oferta.PeriodoId != periodo.Id ||
                    reglas.Tipo != "Reglas" || oferta.Tipo != "Oferta" || reglas.Formato != 1 || oferta.Formato != 1)
                    throw new InvalidOperationException("Las versiones del período no tienen un formato compatible.");
                DatosReglasPeriodo datosReglas;
                ConsultarOfertaRespuesta[] datosOferta;
                try
                {
                    datosReglas = reglas.Datos.Deserialize<DatosReglasPeriodo>() ?? throw new InvalidOperationException("Reglas vacías.");
                    datosOferta = oferta.Datos.Deserialize<ConsultarOfertaRespuesta[]>() ?? throw new InvalidOperationException("Oferta vacía.");
                }
                catch (JsonException) { throw new InvalidOperationException("No se pudieron interpretar las versiones publicadas."); }
                if (datosReglas.Perfil != "Prototipo-2026-1" || datosReglas.FechaCorte == default ||
                    datosOferta.Any(o => o is null || o.PeriodoId != periodo.Id))
                    throw new InvalidOperationException("La configuración publicada no es compatible con el registro de solicitudes.");
                return await operacion(new DatosPeriodoSolicitud(
                    periodo.InstitucionId,
                    periodo.Id,
                    periodo.Estado,
                    periodo.Revision,
                    reglas.Id,
                    oferta.Id,
                    datosReglas.FechaCorte,
                    datosOferta.Where(o => o.Disponible).Select(o => o.TurnoId).Distinct().ToArray()), token);
            },
            tokenCancelacion);
    // Implementa IEjecucionPeriodoSolicitud.EjecutarRevisionAsync: revisar no reinterpreta la configuración publicada.
    public Task<T> EjecutarRevisionAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Func<string, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        return _periodos.EjecutarConPeriodoProtegidoAsync(
            periodoId,
            (periodo, token) => periodo.InstitucionId == institucionId
                ? operacion(periodo.Estado, token)
                : throw new KeyNotFoundException("El período no pertenece a esta institución."),
            tokenCancelacion);
    }
}
