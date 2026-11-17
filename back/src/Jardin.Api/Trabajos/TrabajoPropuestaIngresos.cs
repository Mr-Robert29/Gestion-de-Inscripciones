using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Microsoft.AspNetCore.Http;

namespace Jardin.Api.Trabajos;

public sealed record EstadoTrabajoPropuestaIngresos(
    Guid Id,
    Guid PeriodoId,
    Guid UsuarioId,
    Guid InstitucionId,
    string Estado,
    string Mensaje,
    Guid? PropuestaId,
    DateTimeOffset ActualizadoEn);

public sealed record SolicitudTrabajoPropuestaIngresos(
    Guid TrabajoId,
    Guid PeriodoId,
    GenerarPropuestaIngresosPeticion Peticion,
    AutorizacionVacantes Autor);

public sealed class ColaPropuestasIngresos
{
    private readonly ConcurrentDictionary<Guid, EstadoTrabajoPropuestaIngresos> _trabajos = new();
    private readonly Channel<SolicitudTrabajoPropuestaIngresos> _canal = Channel.CreateBounded<SolicitudTrabajoPropuestaIngresos>(
        new BoundedChannelOptions(50) { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });

    public bool Encolar(Guid periodoId, GenerarPropuestaIngresosPeticion peticion, AutorizacionVacantes autor, out Guid trabajoId)
    {
        LimpiarHistorialAntiguo();
        trabajoId = Guid.NewGuid();
        var ahora = DateTimeOffset.UtcNow;
        _trabajos[trabajoId] = new EstadoTrabajoPropuestaIngresos(
            trabajoId, periodoId, autor.UsuarioId, autor.InstitucionId,
            "En cola", "La propuesta está esperando para procesarse.", null, ahora);
        if (_canal.Writer.TryWrite(new SolicitudTrabajoPropuestaIngresos(trabajoId, periodoId, peticion, autor)))
            return true;
        _trabajos.TryRemove(trabajoId, out _);
        return false;
    }

    public IAsyncEnumerable<SolicitudTrabajoPropuestaIngresos> LeerAsync(CancellationToken token)
        => _canal.Reader.ReadAllAsync(token);

    public EstadoTrabajoPropuestaIngresos? Obtener(Guid trabajoId)
        => _trabajos.GetValueOrDefault(trabajoId);

    public void MarcarEnProceso(Guid id)
        => Actualizar(id, "En proceso", "Calculando una distribución compatible de aspirantes y vacantes.", null);

    public void MarcarCompletado(Guid id, Guid propuestaId)
        => Actualizar(id, "Completado", "La propuesta está lista para revisar.", propuestaId);

    public void MarcarError(Guid id, string mensaje)
        => Actualizar(id, "Error", mensaje, null);

    private void Actualizar(Guid id, string estado, string mensaje, Guid? propuestaId)
    {
        _trabajos.AddOrUpdate(id,
            _ => throw new InvalidOperationException("No se encontró el trabajo de propuesta."),
            (_, anterior) => anterior with { Estado = estado, Mensaje = mensaje, PropuestaId = propuestaId, ActualizadoEn = DateTimeOffset.UtcNow });
    }

    private void LimpiarHistorialAntiguo()
    {
        var limite = DateTimeOffset.UtcNow.AddHours(-24);
        foreach (var trabajo in _trabajos.Values)
            if (trabajo.Estado is "Completado" or "Error" && trabajo.ActualizadoEn < limite)
                _trabajos.TryRemove(trabajo.Id, out _);
    }
}

public sealed class ProcesadorColaPropuestasIngresos(
    ColaPropuestasIngresos cola,
    IServiceScopeFactory fabricaAmbitos,
    ILogger<ProcesadorColaPropuestasIngresos> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var trabajo in cola.LeerAsync(stoppingToken))
        {
            cola.MarcarEnProceso(trabajo.TrabajoId);
            try
            {
                await using var ambito = fabricaAmbitos.CreateAsyncScope();
                var accesoHttp = ambito.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                var contextoAnterior = accesoHttp.HttpContext;
                accesoHttp.HttpContext = CrearContextoDeTrabajo(trabajo.Autor.UsuarioId, ambito.ServiceProvider);
                try
                {
                    var procesador = ambito.ServiceProvider.GetRequiredService<
                        Jardin.Vacantes.Application.Servicios.IProcesadorPropuestaIngresosEnSegundoPlano>();
                    var propuesta = await procesador.GenerarAutorizadoAsync(
                        trabajo.PeriodoId,
                        trabajo.Peticion,
                        trabajo.Autor,
                        stoppingToken);
                    cola.MarcarCompletado(trabajo.TrabajoId, propuesta.Id);
                }
                finally
                {
                    accesoHttp.HttpContext = contextoAnterior;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                cola.MarcarError(trabajo.TrabajoId, "El servidor se detuvo antes de terminar. Volvé a consultar y generar la propuesta.");
                throw;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Falló el trabajo {TrabajoId} al generar una propuesta para el período {PeriodoId}.", trabajo.TrabajoId, trabajo.PeriodoId);
                cola.MarcarError(trabajo.TrabajoId, error is InvalidOperationException or ArgumentException
                    ? error.Message
                    : "No se pudo generar la propuesta por un error interno. Revisá el servidor e intentá nuevamente.");
            }
        }
    }

    private static HttpContext CrearContextoDeTrabajo(Guid usuarioId, IServiceProvider servicios)
    {
        var identidad = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim("sub", usuarioId.ToString())
            ],
            authenticationType: "TrabajoAutorizado");
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identidad),
            RequestServices = servicios
        };
    }
}
