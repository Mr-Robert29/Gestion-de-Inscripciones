using System.Text.Json;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.ObjetosDeValor;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioSolicitudes : IModuloSolicitudes
{
    private readonly IRepositorioSolicitudes _solicitudes;
    private readonly IRepositorioPersonasInscripciones _personas;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    private readonly IEjecucionPeriodoSolicitud _periodo;

    public ServicioSolicitudes(
        IRepositorioSolicitudes solicitudes,
        IRepositorioPersonasInscripciones personas,
        IAccesoInstitucionesInscripciones acceso,
        IEjecucionPeriodoSolicitud periodo)
    {
        _solicitudes = solicitudes;
        _personas = personas;
        _acceso = acceso;
        _periodo = periodo;
    }

    // Implementa IModuloSolicitudes.RegistrarAsync: el estado y las versiones se deciden dentro de la operación protegida.
    public async Task<RegistrarSolicitudRespuesta> RegistrarAsync(
        Guid institucionId,
        Guid periodoId,
        RegistrarSolicitudPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        var autor = await _acceso.AutorizarAsync(
            institucionId,
            true,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentNullException.ThrowIfNull(peticion.Domicilio);
        return await _periodo.EjecutarAsync(
            institucionId,
            periodoId,
            async (periodo, token) =>
            {
                if (periodo.Estado is not ("RecepcionAbierta" or "RecepcionCerrada"))
                    throw new InvalidOperationException("El período todavía no admite recepción de solicitudes.");
                var aspirante = await _personas.ObtenerAspiranteAsync(
                    institucionId,
                    peticion.AspiranteId,
                    token) ?? throw new KeyNotFoundException("No se encontró el aspirante en esta institución.");
                var alternativas = peticion.TurnosAlternativosAceptados ?? [];
                if (!periodo.TurnosDisponibles.Contains(peticion.TurnoPreferidoId) ||
                    alternativas.Any(id => !periodo.TurnosDisponibles.Contains(id)))
                    throw new ArgumentException("Los turnos deben estar habilitados en la oferta publicada del período.");
                var domicilio = peticion.Domicilio;
                var solicitud = new SolicitudInscripcion(
                    institucionId,
                    periodoId,
                    aspirante,
                    new Domicilio(
                        domicilio.Calle,
                        domicilio.Numero,
                        domicilio.Localidad,
                        domicilio.Provincia,
                        domicilio.CodigoPostal,
                        domicilio.Complemento,
                        domicilio.Piso,
                        domicilio.Departamento,
                        domicilio.Barrio),
                    periodo.FechaCorte,
                    peticion.TurnoPreferidoId,
                    alternativas,
                    periodo.VersionReglasId,
                    periodo.VersionOfertaId,
                    periodo.Revision,
                    periodo.Estado == "RecepcionCerrada",
                    autor,
                    peticion.ReferenciaConsentimiento);
                var cambio = new CambioInscripciones(
                    institucionId,
                    autor,
                    solicitud.Id,
                    "Solicitud.Registro",
                    JsonSerializer.Serialize(MapeoSolicitudes.Convertir(solicitud)));
                var alertas = await _solicitudes.AgregarAsync(
                    solicitud,
                    cambio,
                    token);
                return new RegistrarSolicitudRespuesta(
                    solicitud.Id,
                    solicitud.EstadoRecepcion.ToString(),
                    solicitud.CalculoEdad.Sala,
                    solicitud.RequiereRevisionEdad,
                    alertas);
            },
            tokenCancelacion);
    }

    // Implementa IModuloSolicitudes.ConsultarAsync: consulta lo guardado sin recalcular con reglas actuales.
    public async Task<ConsultarSolicitudRespuesta?> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        var solicitud = await _solicitudes.ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        return solicitud is null ? null : MapeoSolicitudes.Convertir(solicitud);
    }

    // Implementa IModuloSolicitudes.ListarAsync.
    public async Task<ListarSolicitudesRespuesta> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        ListarSolicitudesPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(
            institucionId,
            false,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(peticion);
        if (peticion.Pagina < 1 || peticion.TamanoPagina is < 1 or > 100 ||
            (long)(peticion.Pagina - 1) * peticion.TamanoPagina > int.MaxValue)
            throw new ArgumentException("La página debe ser positiva y su tamaño debe estar entre 1 y 100.");
        var solicitudes = await _solicitudes.ListarAsync(
            institucionId,
            periodoId,
            (peticion.Pagina - 1) * peticion.TamanoPagina,
            peticion.TamanoPagina + 1,
            tokenCancelacion);
        return new ListarSolicitudesRespuesta(
            solicitudes.Take(peticion.TamanoPagina).Select(MapeoSolicitudes.Convertir).ToArray(),
            peticion.Pagina,
            peticion.TamanoPagina,
            solicitudes.Count > peticion.TamanoPagina);
    }

}
