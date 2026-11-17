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

    public Task<T> EjecutarConReglasAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Func<DatosReglasEdadSolicitud, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => EjecutarConReglasProtegidasAsync(institucionId, periodoId, operacion, tokenCancelacion);

    private async Task<T> EjecutarConReglasProtegidasAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Func<DatosReglasEdadSolicitud, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        return await _periodos.EjecutarConPeriodoProtegidoAsync(
            periodoId,
            async (periodo, token) =>
            {
                if (periodo.InstitucionId != institucionId)
                    throw new KeyNotFoundException("El período no pertenece a esta institución.");
                if (periodo.VersionReglasId is null)
                    throw new InvalidOperationException("Todavía no se puede validar la edad: el período necesita una fecha de corte y reglas de inscripción configuradas.");
                var version = await _versiones.ConsultarAsync(periodo.VersionReglasId.Value, token);
                if (version is null || version.PeriodoId != periodoId || version.Tipo != "Reglas" || version.Formato != 1)
                    throw new InvalidOperationException("Las reglas publicadas del período no tienen un formato compatible.");
                DatosReglasPeriodo reglas;
                try
                {
                    reglas = version.Datos.Deserialize<DatosReglasPeriodo>() ?? throw new InvalidOperationException("Reglas vacías.");
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException("No se pudieron interpretar las reglas publicadas del período.");
                }
                if (reglas.FechaCorte == default || reglas.CombinacionesEdades is null || reglas.CombinacionesEdades.Count == 0 ||
                    reglas.CombinacionesEdades.Any(edades => edades is null))
                    throw new InvalidOperationException("Las reglas del período no indican una fecha de corte y edades admitidas.");
                var edades = reglas.CombinacionesEdades.SelectMany(x => x).Distinct().Order().ToArray();
                if (edades.Length == 0)
                    throw new InvalidOperationException("Las reglas del período no contienen edades admitidas.");
                return await operacion(new DatosReglasEdadSolicitud(reglas.FechaCorte, edades), token);
            },
            tokenCancelacion);
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
                    datosReglas.CombinacionesEdades is null || datosReglas.CombinacionesEdades.Count == 0 ||
                    datosReglas.CombinacionesEdades.Any(edades => edades is null) ||
                    datosOferta.Any(o => o is null || o.PeriodoId != periodo.Id))
                    throw new InvalidOperationException("La configuración publicada no es compatible con el registro de solicitudes.");
                var edadesAdmitidas = datosReglas.CombinacionesEdades.SelectMany(edades => edades).Distinct().Order().ToArray();
                if (edadesAdmitidas.Length == 0)
                    throw new InvalidOperationException("Las reglas del período no contienen edades admitidas.");
                return await operacion(new DatosPeriodoSolicitud(
                    periodo.InstitucionId,
                    periodo.Id,
                    periodo.Estado,
                    periodo.Revision,
                    reglas.Id,
                    oferta.Id,
                    datosReglas.FechaCorte,
                    datosOferta.Where(o => o.Disponible).Select(o => o.TurnoId).Distinct().ToArray(),
                    edadesAdmitidas), token);
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
    // Implementa IEjecucionPeriodoSolicitud.EjecutarCorreccionAsync: conserva la oferta histórica de la solicitud.
    public Task<T> EjecutarCorreccionAsync<T>(
        Guid institucionId,
        Guid periodoId,
        Guid versionReglasId,
        Guid versionOfertaId,
        Func<DatosPeriodoCorreccionSolicitud, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        return _periodos.EjecutarConPeriodoProtegidoAsync(
            periodoId,
            async (periodo, token) =>
            {
                if (periodo.InstitucionId != institucionId) throw new KeyNotFoundException("El período no pertenece a esta institución.");
                var version = await _versiones.ConsultarAsync(versionOfertaId, token);
                var versionReglas = await _versiones.ConsultarAsync(versionReglasId, token);
                if (version is null || version.PeriodoId != periodoId || version.Tipo != "Oferta" || version.Formato != 1 ||
                    versionReglas is null || versionReglas.PeriodoId != periodoId || versionReglas.Tipo != "Reglas" || versionReglas.Formato != 1)
                    throw new InvalidOperationException("Las versiones históricas de la solicitud no son compatibles.");
                ConsultarOfertaRespuesta[] oferta;
                DatosReglasPeriodo reglas;
                try
                {
                    oferta = version.Datos.Deserialize<ConsultarOfertaRespuesta[]>() ?? throw new InvalidOperationException("Oferta vacía.");
                    reglas = versionReglas.Datos.Deserialize<DatosReglasPeriodo>() ?? throw new InvalidOperationException("Reglas vacías.");
                }
                catch (JsonException) { throw new InvalidOperationException("No se pudo interpretar la oferta publicada."); }
                if (oferta.Any(o => o is null || o.PeriodoId != periodoId) || reglas.FechaCorte == default ||
                    reglas.CombinacionesEdades is null || reglas.CombinacionesEdades.Count == 0 ||
                    reglas.CombinacionesEdades.Any(edades => edades is null))
                    throw new InvalidOperationException("La configuración histórica de la solicitud no es compatible.");
                var edadesAdmitidas = reglas.CombinacionesEdades.SelectMany(edades => edades).Distinct().Order().ToArray();
                if (edadesAdmitidas.Length == 0)
                    throw new InvalidOperationException("Las reglas históricas no contienen edades admitidas.");
                return await operacion(new DatosPeriodoCorreccionSolicitud(
                    periodo.Estado,
                    reglas.FechaCorte,
                    edadesAdmitidas,
                    oferta.Where(o => o.Disponible).Select(o => o.TurnoId).Distinct().ToArray()), token);
            },
            tokenCancelacion);
    }
}
