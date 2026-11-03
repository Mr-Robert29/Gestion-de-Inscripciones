using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

internal static class MapeoSolicitudes
{
    public static ConsultarSolicitudRespuesta Convertir(SolicitudInscripcion solicitud)
    {
        var domicilio = solicitud.DomicilioDeclarado;
        return new ConsultarSolicitudRespuesta(
            solicitud.Id,
            solicitud.InstitucionId,
            solicitud.PeriodoId,
            solicitud.AspiranteId,
            new DatosDomicilioSolicitud(
                domicilio.Calle,
                domicilio.Numero,
                domicilio.Localidad,
                domicilio.Provincia,
                domicilio.CodigoPostal,
                domicilio.Complemento,
                domicilio.Piso,
                domicilio.Departamento,
                domicilio.Barrio),
            solicitud.CalculoEdad.FechaNacimientoUtilizada,
            solicitud.CalculoEdad.FechaCorteUtilizada,
            solicitud.CalculoEdad.EdadCalculada,
            solicitud.CalculoEdad.Sala,
            solicitud.RequiereRevisionEdad,
            solicitud.TurnoPreferidoId,
            solicitud.TurnosAlternativos.Select(t => t.TurnoId).Order().ToArray(),
            solicitud.VersionReglasId,
            solicitud.VersionOfertaId,
            solicitud.RevisionPeriodoUtilizada,
            solicitud.EstadoRecepcion.ToString(),
            solicitud.FueraDeTermino,
            solicitud.UsuarioRegistroId,
            solicitud.FechaRegistro,
            solicitud.ReferenciaConsentimiento,
            solicitud.RevisionDatos,
            solicitud.RevisionDomicilio);
    }
}
