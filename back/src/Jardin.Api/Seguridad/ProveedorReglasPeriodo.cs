using System.Text.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Vacantes.Application.Seguridad;

namespace Jardin.Api.Seguridad;

public sealed class ProveedorReglasPeriodo : IProveedorReglasPeriodo
{
    private readonly IModuloPeriodos _periodos;
    private readonly IModuloVersionesConfiguracion _versiones;

    public ProveedorReglasPeriodo(
        IModuloPeriodos periodos,
        IModuloVersionesConfiguracion versiones)
    {
        _periodos = periodos;
        _versiones = versiones;
    }

    public async Task<ReglasPeriodoVacantes> ConsultarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var periodo = await _periodos.ConsultarPeriodoAsync(periodoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el período.");
        if (periodo.VersionReglasId is null)
            throw new InvalidOperationException("El período no tiene una versión de reglas asignada.");
        var version = await _versiones.ConsultarAsync(periodo.VersionReglasId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró la versión de reglas del período.");
        if (version.PeriodoId != periodoId || version.Tipo != "Reglas" || version.Formato != 1)
            throw new InvalidOperationException("La versión de reglas no es compatible con el período.");
        var datos = version.Datos.Deserialize<DatosReglasPeriodo>()
            ?? throw new InvalidOperationException("La versión de reglas está vacía.");
        if (datos.OrdenGruposEtarios.Count == 0 || datos.OrdenProcedimiento.Count == 0)
            throw new InvalidOperationException("La versión de reglas no define el orden de evaluación.");
        if (datos.PrecisionDistanciaDecimales is < 1 or > 2)
            throw new InvalidOperationException("La precisión de distancia debe estar entre uno y dos decimales.");
        return new ReglasPeriodoVacantes(
            version.Id,
            datos.OrdenGruposEtarios,
            datos.OrdenProcedimiento,
            datos.PrecisionDistanciaDecimales);
    }
}
