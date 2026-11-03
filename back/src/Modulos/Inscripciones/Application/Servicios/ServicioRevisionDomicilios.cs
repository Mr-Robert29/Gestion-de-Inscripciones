using System.Text.Json;
using System.Text.Json.Serialization;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioRevisionDomicilios : IModuloRevisionDomicilios
{
    private static readonly JsonSerializerOptions OpcionesHistorial = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IRepositorioRevisionDomicilios _revisiones;
    private readonly IRepositorioSolicitudes _solicitudes;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    private readonly IEjecucionPeriodoSolicitud _periodo;
    public ServicioRevisionDomicilios(
        IRepositorioRevisionDomicilios revisiones,
        IRepositorioSolicitudes solicitudes,
        IAccesoInstitucionesInscripciones acceso,
        IEjecucionPeriodoSolicitud periodo)
    {
        _revisiones = revisiones;
        _solicitudes = solicitudes;
        _acceso = acceso;
        _periodo = periodo;
    }

    // Implementa IModuloRevisionDomicilios.ConsultarAsync: una dirección nueva no hereda la revisión de la anterior.
    public async Task<ConsultarRevisionDomicilioRespuesta> ConsultarAsync(
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
        var historial = await _revisiones.ConsultarAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        return Mapear(solicitud, historial);
    }

    // Implementa IModuloRevisionDomicilios.RegistrarAsync: revisar no cambia la dirección ni calcula distancias.
    public async Task<ConsultarRevisionDomicilioRespuesta> RegistrarAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarRevisionDomicilioPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.RevisarDomicilio,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var nombre = Enum.GetNames<EstadoRevisionDomicilio>().SingleOrDefault(n => string.Equals(
            n,
            peticion.Estado?.Trim(),
            StringComparison.OrdinalIgnoreCase));
        if (nombre is null) throw new ArgumentException("Usá PendienteRevision, Observado o RevisadoManualmente.");
        var estado = Enum.Parse<EstadoRevisionDomicilio>(nombre);
        if (peticion.RevisionSolicitud < 1 || peticion.RevisionDomicilio < 1 || peticion.UltimoNumeroRegistro < 0)
            throw new ArgumentException("Las revisiones y el último número no son válidos.");
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
                var historial = await _revisiones.ConsultarAsync(
                    institucionId,
                    solicitudId,
                    token);
                var ultimo = historial.FirstOrDefault()?.NumeroRegistro ?? 0;
                if (actual.RevisionDatos != peticion.RevisionSolicitud || actual.RevisionDomicilio != peticion.RevisionDomicilio ||
                    ultimo != peticion.UltimoNumeroRegistro) throw new ConflictoRevisionSolicitudException();
                var revision = new RevisionManualDomicilio(actual, new DatosDecisionRevisionDomicilio(
                    estado,
                    checked(ultimo + 1),
                    autor,
                    peticion.Motivo,
                    peticion.Referencia));
                await _revisiones.GuardarAsync(
                    revision,
                    new CambioInscripciones(
                        institucionId,
                        autor,
                        revision.Id,
                        "Domicilio.RevisionManual",
                        JsonSerializer.Serialize(new { Anterior = Mapear(actual, historial).RevisionActual, Nueva = revision }, OpcionesHistorial)),
                    token);
                return await ConsultarAsync(
                    institucionId,
                    solicitudId,
                    token);
            },
            tokenCancelacion);
    }

    private async Task<SolicitudInscripcion> ObtenerAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => await _solicitudes.ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró la solicitud en esta institución.");

    private static ConsultarRevisionDomicilioRespuesta Mapear(SolicitudInscripcion solicitud, IReadOnlyList<RevisionManualDomicilio> historial)
    {
        var ultimo = historial.FirstOrDefault()?.NumeroRegistro ?? 0;
        var datos = historial.Select(r =>
        {
            var d = r.Domicilio;
            return new DatosRevisionDomicilio(
                r.Id,
                r.NumeroRegistro,
                r.RevisionDomicilio,
                r.RevisionSolicitudUtilizada,
                r.Estado.ToString(),
                new DatosDomicilioSolicitud(
                    d.Calle,
                    d.Numero,
                    d.Localidad,
                    d.Provincia,
                    d.CodigoPostal,
                    d.Complemento,
                    d.Piso,
                    d.Departamento,
                    d.Barrio),
                r.UsuarioId,
                r.Fecha,
                r.Motivo,
                r.Referencia,
                r.NumeroRegistro == ultimo && r.RevisionDomicilio == solicitud.RevisionDomicilio);
        }).ToArray();
        var actual = datos.SingleOrDefault(d => d.EsActual);
        return new ConsultarRevisionDomicilioRespuesta(
            solicitud.Id,
            solicitud.RevisionDatos,
            solicitud.RevisionDomicilio,
            actual?.Estado ?? nameof(EstadoRevisionDomicilio.PendienteRevision),
            ultimo,
            actual,
            datos);
    }
}
