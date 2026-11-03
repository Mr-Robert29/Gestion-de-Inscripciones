using System.Text.Json;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Geografia;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioDistancias : IModuloDistancias
{
    private readonly IRepositorioDistancias _distancias;
    private readonly IRepositorioSolicitudes _solicitudes;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    private readonly IUbicacionJardin _jardin;
    private readonly IEjecucionPeriodoSolicitud _periodo;
    public ServicioDistancias(
        IRepositorioDistancias distancias,
        IRepositorioSolicitudes solicitudes,
        IAccesoInstitucionesInscripciones acceso,
        IUbicacionJardin jardin,
        IEjecucionPeriodoSolicitud periodo)
    {
        _distancias = distancias;
        _solicitudes = solicitudes;
        _acceso = acceso;
        _jardin = jardin;
        _periodo = periodo;
    }

    // Implementa IModuloDistancias.RegistrarManualAsync: registra un dato provisional aportado por personal autorizado.
    public async Task<ConsultarDistanciaRespuesta> RegistrarManualAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarDistanciaManualPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.RegistrarDistanciaManual,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.DistanciaMetros is null or < 0) throw new ArgumentException("Indicá una distancia no negativa en metros; no se reemplaza un dato ausente por cero.");
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Referencia);
        if (peticion.UltimoNumeroRegistro < 0 || peticion.RevisionDomicilio < 1 || peticion.RevisionUbicacionJardin < 0)
            throw new ArgumentException("Las revisiones y el último número de registro no son válidos.");
        var solicitud = await ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        return await _periodo.EjecutarRevisionAsync(
            institucionId,
            solicitud.PeriodoId,
            async (_, token) =>
            {
                var actual = await ObtenerAsync(
                    institucionId,
                    solicitudId,
                    token);
                var jardin = await _jardin.ConsultarAsync(institucionId, token);
                var historial = await _distancias.ConsultarAsync(
                    institucionId,
                    solicitudId,
                    token);
                var ultimo = historial.FirstOrDefault()?.NumeroRegistro ?? 0;
                if (peticion.RevisionDomicilio != actual.RevisionDomicilio || peticion.RevisionUbicacionJardin != jardin.RevisionUbicacion ||
                    peticion.UltimoNumeroRegistro != ultimo) throw new ConflictoRevisionSolicitudException();
                var medicion = new MedicionDistancia(actual, new RegistroDistanciaManual(
                    peticion.DistanciaMetros.Value,
                    jardin.RevisionUbicacion,
                    checked(ultimo + 1),
                    autor,
                    peticion.Referencia,
                    JsonSerializer.Serialize(new
                    {
                        DomicilioSolicitud = actual.DomicilioDeclarado,
                        DomicilioJardin = jardin.Domicilio,
                        CoordenadasJardin = jardin.Coordenadas,
                        ReglaComparacion = "Metros ingresados manualmente, sin redondeo. Precisión física no verificada."
                    })));
                await _distancias.GuardarAsync(
                    medicion,
                    new CambioInscripciones(
                        institucionId,
                        autor,
                        medicion.Id,
                        "Distancia.RegistroManual",
                        JsonSerializer.Serialize(new { AnteriorId = historial.FirstOrDefault()?.Id, Nuevo = medicion })),
                    token);
                return Mapear(
                    medicion,
                    medicion.NumeroRegistro,
                    actual.RevisionDomicilio,
                    jardin.RevisionUbicacion);
            },
            tokenCancelacion);
    }

    // Implementa IModuloDistancias.ConsultarHistorialAsync: distingue registro reemplazado y entradas desactualizadas.
    public async Task<IReadOnlyList<ConsultarDistanciaRespuesta>> ConsultarHistorialAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        var solicitud = await ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        var jardin = await _jardin.ConsultarAsync(institucionId, tokenCancelacion);
        var historial = await _distancias.ConsultarAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        var ultimo = historial.FirstOrDefault()?.NumeroRegistro ?? 0;
        return historial.Select(m => Mapear(
            m,
            ultimo,
            solicitud.RevisionDomicilio,
            jardin.RevisionUbicacion)).ToArray();
    }

    private async Task<SolicitudInscripcion> ObtenerAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => await _solicitudes.ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró la solicitud en esta institución.");

    private static ConsultarDistanciaRespuesta Mapear(
        MedicionDistancia medicion,
        int ultimo,
        int revisionDomicilio,
        int revisionJardin)
    {
        using var entradas = JsonDocument.Parse(medicion.Entradas);
        return new ConsultarDistanciaRespuesta(
            medicion.Id,
            medicion.SolicitudId,
            medicion.NumeroRegistro,
            medicion.DistanciaMetros,
            medicion.Origen,
            medicion.Referencia,
            medicion.RevisionDomicilio,
            medicion.RevisionUbicacionJardin,
            medicion.FechaRegistro,
            medicion.UsuarioId,
            entradas.RootElement.Clone(),
            medicion.NumeroRegistro == ultimo,
            medicion.NumeroRegistro == ultimo && medicion.RevisionDomicilio == revisionDomicilio && medicion.RevisionUbicacionJardin == revisionJardin);
    }
}
