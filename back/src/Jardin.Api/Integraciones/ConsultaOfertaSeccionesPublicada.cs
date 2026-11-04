using System.Text.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Secciones.Application.Abstracciones;

namespace Jardin.Api.Integraciones;

// Secciones utiliza la misma oferta y combinaciones publicadas que Vacantes.
public sealed class ConsultaOfertaSeccionesPublicada : IProveedorOfertaSecciones
{
    private readonly IModuloPeriodos _periodos;
    private readonly IModuloVersionesConfiguracion _versiones;
    public ConsultaOfertaSeccionesPublicada(IModuloPeriodos periodos, IModuloVersionesConfiguracion versiones)
    {
        _periodos = periodos;
        _versiones = versiones;
    }

    // Implementa IProveedorOfertaSecciones.ObtenerAsync mediante contratos públicos de Configuración.
    public async Task<OfertaSeccionConfigurada?> ObtenerAsync(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        CancellationToken cancellationToken = default)
    {
        var periodo = await _periodos.ConsultarPeriodoAsync(periodoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el período.");
        if (periodo.VersionOfertaId is null || periodo.VersionReglasId is null)
            throw new InvalidOperationException("El período requiere reglas y oferta publicadas.");
        var oferta = await _versiones.ConsultarAsync(periodo.VersionOfertaId.Value, cancellationToken);
        var reglas = await _versiones.ConsultarAsync(periodo.VersionReglasId.Value, cancellationToken);
        if (oferta is null || reglas is null || oferta.PeriodoId != periodoId || reglas.PeriodoId != periodoId ||
            oferta.Tipo != "Oferta" || reglas.Tipo != "Reglas" || oferta.Formato != 1 || reglas.Formato != 1)
            throw new InvalidOperationException("Las versiones de oferta y reglas no son compatibles.");
        var recurso = (oferta.Datos.Deserialize<ConsultarOfertaRespuesta[]>() ?? [])
            .SingleOrDefault(x => x.AulaId == aulaId && x.TurnoId == turnoId && x.Disponible);
        if (recurso is null || recurso.Capacidad <= 0) return null;
        var perfil = reglas.Datos.Deserialize<DatosReglasPeriodo>()
            ?? throw new InvalidOperationException("La versión de reglas no contiene sus combinaciones.");
        return new OfertaSeccionConfigurada(
            periodoId,
            aulaId,
            turnoId,
            recurso.Capacidad,
            perfil.CombinacionesEdades.Select(x => (IReadOnlyList<int>)x).ToArray());
    }
}
