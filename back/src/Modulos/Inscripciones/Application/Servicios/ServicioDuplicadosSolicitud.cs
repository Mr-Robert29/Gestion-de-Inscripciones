using System.Text.Json;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.Servicios;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioDuplicadosSolicitud : IModuloDuplicadosSolicitud
{
    private readonly IRepositorioDuplicadosSolicitud _duplicados;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    private readonly IEjecucionPeriodoSolicitud _periodo;

    public ServicioDuplicadosSolicitud(
        IRepositorioDuplicadosSolicitud duplicados,
        IAccesoInstitucionesInscripciones acceso,
        IEjecucionPeriodoSolicitud periodo)
    {
        _duplicados = duplicados;
        _acceso = acceso;
        _periodo = periodo;
    }

    // Implementa IModuloDuplicadosSolicitud.DetectarAsync: revisar datos existentes no confirma una duplicación.
    public async Task<DetectarDuplicadosRespuesta> DetectarAsync(
        Guid institucionId,
        Guid periodoId,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.DetectarDuplicados,
            tokenCancelacion);
        return await _periodo.EjecutarRevisionAsync(
            institucionId,
            periodoId,
            (_, token) => _duplicados.DetectarAsync(
                institucionId,
                periodoId,
                autor,
                token),
            tokenCancelacion);
    }

    // Implementa IModuloDuplicadosSolicitud.ListarAsync.
    public async Task<ListarDuplicadosRespuesta> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        ListarDuplicadosPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.Pagina < 1 || peticion.TamanoPagina is < 1 or > 100 ||
            (long)(peticion.Pagina - 1) * peticion.TamanoPagina > int.MaxValue)
            throw new ArgumentException("La página debe ser positiva y su tamaño debe estar entre 1 y 100.");
        EstadoPosibleDuplicado? estado = null;
        if (!string.IsNullOrWhiteSpace(peticion.Estado))
        {
            var nombre = Enum.GetNames<EstadoPosibleDuplicado>().SingleOrDefault(n => string.Equals(
                n,
                peticion.Estado.Trim(),
                StringComparison.OrdinalIgnoreCase));
            estado = nombre is null ? throw new ArgumentException("El estado no es válido.") : Enum.Parse<EstadoPosibleDuplicado>(nombre);
        }
        var alertas = await _duplicados.ListarAsync(
            institucionId,
            periodoId,
            estado,
            (peticion.Pagina - 1) * peticion.TamanoPagina,
            peticion.TamanoPagina + 1,
            tokenCancelacion);
        return new ListarDuplicadosRespuesta(
            alertas.Take(peticion.TamanoPagina).Select(Mapear).ToArray(),
            peticion.Pagina,
            peticion.TamanoPagina,
            alertas.Count > peticion.TamanoPagina);
    }

    // Implementa IModuloDuplicadosSolicitud.ConsultarAsync.
    public async Task<ConsultarDuplicadoRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid alertaId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        return Mapear(await ObtenerAsync(
            institucionId,
            alertaId,
            tokenCancelacion));
    }

    // Implementa IModuloDuplicadosSolicitud.ResolverAsync: conserva ambas solicitudes y la decisión administrativa.
    public async Task<ResolverDuplicadoRespuesta> ResolverAsync(
        Guid institucionId,
        Guid alertaId,
        ResolverDuplicadoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.ResolverDuplicados,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var decision = peticion.Decision?.Trim();
        var estado = string.Equals(decision, "DuplicacionConfirmada", StringComparison.OrdinalIgnoreCase)
            ? EstadoPosibleDuplicado.DuplicacionConfirmada
            : string.Equals(decision, "PersonasDiferentes", StringComparison.OrdinalIgnoreCase)
                ? EstadoPosibleDuplicado.PersonasDiferentes
                : throw new ArgumentException("Seleccioná DuplicacionConfirmada o PersonasDiferentes.");
        var alerta = await ObtenerAsync(
            institucionId,
            alertaId,
            tokenCancelacion);
        return await _periodo.EjecutarRevisionAsync(
            institucionId,
            alerta.PeriodoId,
            async (_, token) =>
            {
                if (peticion.Revision != alerta.Revision) throw new ConflictoDuplicadoException();
                var actuales = await _duplicados.LeerParejaActualAsync(alerta, token);
                if (actuales.Count != 2 || !alerta.CoincidenDatos(actuales[0], actuales[1])) throw new ConflictoDuplicadoException();
                var anteriores = Mapear(alerta);
                alerta.Resolver(
                    estado,
                    autor,
                    peticion.Motivo);
                await _duplicados.GuardarResolucionAsync(
                    alerta,
                    new CambioInscripciones(
                        institucionId,
                        autor,
                        alerta.Id,
                        "Duplicado.Resolucion",
                        JsonSerializer.Serialize(new { Anteriores = anteriores, Nuevos = Mapear(alerta) })),
                    token);
                return new ResolverDuplicadoRespuesta(
                    alerta.Id,
                    alerta.Estado.ToString(),
                    alerta.Revision);
            },
            tokenCancelacion);
    }

    private async Task<PosibleDuplicado> ObtenerAsync(
        Guid institucionId,
        Guid alertaId,
        CancellationToken tokenCancelacion)
        => await _duplicados.ObtenerAsync(
            institucionId,
            alertaId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró la alerta en esta institución.");

    private static ConsultarDuplicadoRespuesta Mapear(PosibleDuplicado alerta)
    {
        using var datos = JsonDocument.Parse(alerta.DatosEvaluados);
        return new ConsultarDuplicadoRespuesta(
            alerta.Id,
            alerta.InstitucionId,
            alerta.PeriodoId,
            alerta.SolicitudAId,
            alerta.SolicitudBId,
            alerta.PerfilDeteccion,
            Enum.GetValues<CriterioCoincidenciaSolicitud>().Where(c => c != CriterioCoincidenciaSolicitud.Ninguno && alerta.Criterios.HasFlag(c))
                .Select(c => c.ToString()).ToArray(),
            datos.RootElement.Clone(),
            alerta.Estado.ToString(),
            alerta.Revision,
            alerta.UsuarioDeteccionId,
            alerta.FechaDeteccion,
            alerta.UsuarioUltimaDeteccionId,
            alerta.FechaUltimaDeteccion,
            alerta.UsuarioResolucionId,
            alerta.FechaResolucion,
            alerta.MotivoResolucion);
    }
}
