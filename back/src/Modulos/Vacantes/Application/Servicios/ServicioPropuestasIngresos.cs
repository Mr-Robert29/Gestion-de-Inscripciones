using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioPropuestasIngresos : IModuloPropuestasIngresos
{
    private readonly IAutorizacionPropuestasIngresos _autorizacion;
    private readonly IContextoPropuestaIngresos _contexto;
    private readonly IRepositorioPropuestasIngresos _propuestas;
    private readonly IRepositorioSorteos _sorteos;
    private readonly IRepositorioCasosRevision _casos;

    public ServicioPropuestasIngresos(
        IAutorizacionPropuestasIngresos autorizacion,
        IContextoPropuestaIngresos contexto,
        IRepositorioPropuestasIngresos propuestas,
        IRepositorioSorteos sorteos,
        IRepositorioCasosRevision casos)
    {
        _autorizacion = autorizacion;
        _contexto = contexto;
        _propuestas = propuestas;
        _sorteos = sorteos;
        _casos = casos;
    }

    // Implementa IModuloPropuestasIngresos.GenerarAsync: coordina una nueva propuesta, sin confirmar ingresos.
    public async Task<ConsultarPropuestaIngresosRespuesta> GenerarAsync(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var autor = await _autorizacion.AutorizarPropuestaAsync(periodoId, tokenCancelacion);
        return await _contexto.ConPeriodoProtegidoAsync(
            periodoId,
            async token =>
            {
                var datos = await LeerAsync(periodoId, token);
                if (datos.Entradas.EstadoPeriodo != "RecepcionCerrada")
                    throw new InvalidOperationException("Cerrá la recepción antes de generar una propuesta.");
                if (datos.Entradas.InstitucionId != autor.InstitucionId)
                    throw new UnauthorizedAccessException();
                var resultados = PlanificadorPropuestaIngresos.Generar(
                    datos.Entradas,
                    datos.Sorteos,
                    datos.Casos,
                    datos.Ocupaciones,
                    token);
                var huella = Huella(datos);
                if (Huella(await LeerAsync(periodoId, token)) != huella)
                    throw new InvalidOperationException("Las entradas cambiaron durante el cálculo. Generá otra propuesta.");
                var propuesta = new PropuestaIngresos(
                    periodoId,
                    autor.InstitucionId,
                    autor.UsuarioId,
                    peticion.Motivo,
                    huella,
                    JsonSerializer.Serialize(datos),
                    JsonSerializer.Serialize(resultados));
                await _propuestas.GuardarAsync(propuesta, token);
                return Mapear(propuesta, true);
            },
            tokenCancelacion);
    }

    // Implementa IModuloPropuestasIngresos.ConsultarAsync: conserva el resultado aun si sus entradas cambiaron.
    public async Task<ConsultarPropuestaIngresosRespuesta?> ConsultarAsync(Guid propuestaId, CancellationToken tokenCancelacion = default)
    {
        var propuesta = await _propuestas.ObtenerAsync(propuestaId, tokenCancelacion);
        if (propuesta is null) return null;
        await _autorizacion.AutorizarPropuestaAsync(propuesta.PeriodoId, tokenCancelacion);
        return Mapear(propuesta, await EstaVigenteAsync(propuesta, tokenCancelacion));
    }

    // Implementa IModuloPropuestasIngresos.ListarAsync: devuelve versiones históricas con su vigencia observada.
    public async Task<IReadOnlyList<ConsultarPropuestaIngresosRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        await _autorizacion.AutorizarPropuestaAsync(periodoId, tokenCancelacion);
        var propuestas = await _propuestas.ListarAsync(periodoId, tokenCancelacion);
        var respuesta = new List<ConsultarPropuestaIngresosRespuesta>();
        foreach (var propuesta in propuestas)
            respuesta.Add(Mapear(propuesta, await EstaVigenteAsync(propuesta, tokenCancelacion)));
        return respuesta;
    }

    private async Task<bool> EstaVigenteAsync(PropuestaIngresos propuesta, CancellationToken tokenCancelacion)
    {
        try { return Huella(await LeerAsync(propuesta.PeriodoId, tokenCancelacion)) == propuesta.HuellaEntradas; }
        catch (InvalidOperationException) { return false; }
        catch (KeyNotFoundException) { return false; }
    }

    private async Task<Instantanea> LeerAsync(Guid periodoId, CancellationToken tokenCancelacion)
        => new(
            await _contexto.LeerAsync(periodoId, tokenCancelacion),
            (await _sorteos.ListarVigentesAsync(periodoId, tokenCancelacion)).OrderBy(x => x.Id).ToArray(),
            (await _casos.ListarAsync(periodoId, tokenCancelacion)).OrderBy(x => x.Id).ToArray(),
            (await _propuestas.ListarOcupacionesAsync(periodoId, tokenCancelacion)).OrderBy(x => x.Id).ToArray());

    private static string Huella(Instantanea datos)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datos))));

    private static ConsultarPropuestaIngresosRespuesta Mapear(PropuestaIngresos propuesta, bool vigente)
    {
        using var instantanea = JsonDocument.Parse(propuesta.Entradas);
        var entradas = instantanea.RootElement.GetProperty("Entradas").Deserialize<EntradasPropuestaIngresos>()!;
        return new ConsultarPropuestaIngresosRespuesta(
            propuesta.Id,
            propuesta.InstitucionId,
            propuesta.PeriodoId,
            propuesta.UsuarioId,
            propuesta.Fecha,
            propuesta.Motivo,
            entradas.VersionReglasId,
            entradas.VersionOfertaId,
            vigente,
            propuesta.HuellaEntradas,
            JsonSerializer.Deserialize<DatosResultadoPropuesta[]>(propuesta.Resultados)!);
    }

    private sealed record Instantanea(
        EntradasPropuestaIngresos Entradas,
        IReadOnlyList<Sorteo> Sorteos,
        IReadOnlyList<CasoRevision> Casos,
        IReadOnlyList<OcupacionPlaza> Ocupaciones);
}
