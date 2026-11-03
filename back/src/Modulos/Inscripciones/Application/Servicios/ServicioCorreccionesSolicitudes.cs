using System.Text.Json;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.ObjetosDeValor;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioCorreccionesSolicitudes : IModuloCorreccionesSolicitudes
{
    private readonly IRepositorioCorreccionesSolicitudes _solicitudes;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    private readonly IEjecucionPeriodoSolicitud _periodo;

    public ServicioCorreccionesSolicitudes(
        IRepositorioCorreccionesSolicitudes solicitudes,
        IAccesoInstitucionesInscripciones acceso,
        IEjecucionPeriodoSolicitud periodo)
    {
        _solicitudes = solicitudes;
        _acceso = acceso;
        _periodo = periodo;
    }

    // Implementa IModuloCorreccionesSolicitudes.BuscarAsync.
    public async Task<BuscarSolicitudesRespuesta> BuscarAsync(
        Guid institucionId,
        BuscarSolicitudesPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.Pagina < 1 || peticion.TamanoPagina is < 1 or > 100 ||
            (long)(peticion.Pagina - 1) * peticion.TamanoPagina > int.MaxValue ||
            peticion.SolicitudId == Guid.Empty || peticion.PeriodoId == Guid.Empty)
            throw new ArgumentException("Revisá los identificadores y la paginación: página positiva, tamaño entre 1 y 100.");
        var resultados = await _solicitudes.BuscarAsync(
            institucionId,
            peticion,
            (peticion.Pagina - 1) * peticion.TamanoPagina,
            peticion.TamanoPagina + 1,
            tokenCancelacion);
        return new BuscarSolicitudesRespuesta(
            resultados.Take(peticion.TamanoPagina).ToArray(),
            peticion.Pagina,
            peticion.TamanoPagina,
            resultados.Count > peticion.TamanoPagina);
    }

    // Implementa IModuloCorreccionesSolicitudes.CorregirAsync.
    public async Task<CorregirSolicitudRespuesta> CorregirAsync(
        Guid institucionId,
        Guid solicitudId,
        CorregirSolicitudPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentNullException.ThrowIfNull(peticion.Domicilio);
        ArgumentNullException.ThrowIfNull(peticion.TurnosAlternativosAceptados);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var solicitud = await _solicitudes.ObtenerEditableAsync(
            institucionId,
            solicitudId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró la solicitud en esta institución.");
        return await _periodo.EjecutarCorreccionAsync(
            institucionId,
            solicitud.PeriodoId,
            solicitud.VersionOfertaId,
            async (periodo, token) =>
            {
                var operacion = periodo.Estado switch
                {
                    "RecepcionAbierta" => OperacionInscripciones.CorregirDuranteRecepcion,
                    "RecepcionCerrada" => OperacionInscripciones.CorregirDespuesDelCierre,
                    _ => throw new InvalidOperationException("El estado del período no permite corregir solicitudes.")
                };
                var autor = await _acceso.AutorizarOperacionAsync(
                    institucionId,
                    operacion,
                    token);
                if (solicitud.RevisionDatos != peticion.RevisionSolicitud) throw new ConflictoRevisionSolicitudException();
                if (!periodo.TurnosDisponibles.Contains(peticion.TurnoPreferidoId) ||
                    peticion.TurnosAlternativosAceptados.Any(id => !periodo.TurnosDisponibles.Contains(id)))
                    throw new ArgumentException("Los turnos deben pertenecer a la versión de oferta utilizada por esta solicitud.");
                var anteriores = MapeoSolicitudes.Convertir(solicitud);
                var d = peticion.Domicilio;
                var cambioDomicilio = solicitud.CorregirDatos(
                    new Domicilio(
                        d.Calle,
                        d.Numero,
                        d.Localidad,
                        d.Provincia,
                        d.CodigoPostal,
                        d.Complemento,
                        d.Piso,
                        d.Departamento,
                        d.Barrio),
                    peticion.TurnoPreferidoId,
                    peticion.TurnosAlternativosAceptados,
                    peticion.ReferenciaConsentimiento);
                await _solicitudes.GuardarAsync(
                    solicitud,
                    new CambioInscripciones(
                        institucionId,
                        autor,
                        solicitud.Id,
                        "Solicitud.Correccion",
                        JsonSerializer.Serialize(new
                        {
                            Motivo = peticion.Motivo.Trim(),
                            Anteriores = anteriores,
                            Nuevos = MapeoSolicitudes.Convertir(solicitud),
                            CambioDomicilio = cambioDomicilio
                        })),
                    token);
                return new CorregirSolicitudRespuesta(
                    solicitud.Id,
                    solicitud.RevisionDatos,
                    solicitud.RevisionDomicilio,
                    cambioDomicilio);
            },
            tokenCancelacion);
    }
}
