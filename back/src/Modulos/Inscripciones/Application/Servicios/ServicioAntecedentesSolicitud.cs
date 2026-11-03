using System.Text.Json;
using Jardin.Inscripciones.Application.Configuracion;
using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioAntecedentesSolicitud : IModuloAntecedentesSolicitud
{
    private readonly IRepositorioAntecedentesSolicitud _antecedentes;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    private readonly IEjecucionPeriodoSolicitud _periodo;

    public ServicioAntecedentesSolicitud(
        IRepositorioAntecedentesSolicitud antecedentes,
        IAccesoInstitucionesInscripciones acceso,
        IEjecucionPeriodoSolicitud periodo)
    {
        _antecedentes = antecedentes;
        _acceso = acceso;
        _periodo = periodo;
    }

    // Implementa IModuloAntecedentesSolicitud.ConsultarAsync: el agregado se lee de forma coherente en una consulta.
    public async Task<ConsultarAntecedentesRespuesta> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        var solicitud = await ObtenerSolicitudAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        return new ConsultarAntecedentesRespuesta(
            solicitud.Id,
            solicitud.RevisionDatos,
            solicitud.Documentos.OrderBy(d => d.Id).Select(MapearDocumento).ToArray(),
            solicitud.Condiciones.OrderBy(c => c.Id).Select(MapearCondicion).ToArray());
    }

    // Implementa IModuloAntecedentesSolicitud.RegistrarDocumentoAsync.
    public Task<RegistrarDocumentoRespuesta> RegistrarDocumentoAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarDocumentoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        return EjecutarCambioAsync(
            institucionId,
            solicitudId,
            OperacionInscripciones.RegistrarDocumentacion,
            async (
                solicitud,
                autor,
                token) =>
            {
                ComprobarRevision(
                    solicitud,
                    peticion.RevisionSolicitud,
                    peticion.Motivo);
                var documento = new DocumentacionSolicitud(
                    solicitud.Id,
                    peticion.Tipo,
                    LeerEstado<EstadoDocumentacionSolicitud>(peticion.Estado),
                    peticion.Referencia,
                    peticion.Observacion,
                    autor);
                var cambio = RegistrarCambio(
                    solicitud,
                    autor,
                    "Documento.Registro",
                    peticion.Motivo,
                    null,
                    MapearDocumento(documento));
                await _antecedentes.AgregarDocumentoAsync(
                    solicitud,
                    documento,
                    cambio,
                    token);
                return new RegistrarDocumentoRespuesta(documento.Id, solicitud.RevisionDatos);
            },
            tokenCancelacion);
    }

    // Implementa IModuloAntecedentesSolicitud.RevisarDocumentoAsync.
    public Task<RevisarAntecedenteRespuesta> RevisarDocumentoAsync(
        Guid institucionId,
        Guid solicitudId,
        Guid documentoId,
        RevisarDocumentoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        return EjecutarCambioAsync(
            institucionId,
            solicitudId,
            OperacionInscripciones.RegistrarDocumentacion,
            async (
                solicitud,
                autor,
                token) =>
            {
                ComprobarRevision(
                    solicitud,
                    peticion.RevisionSolicitud,
                    peticion.Motivo);
                var documento = solicitud.Documentos.SingleOrDefault(d => d.Id == documentoId)
                    ?? throw new KeyNotFoundException("No se encontró el documento en esta solicitud.");
                var anteriores = MapearDocumento(documento);
                documento.Revisar(
                    LeerEstado<EstadoDocumentacionSolicitud>(peticion.Estado),
                    peticion.Referencia,
                    peticion.Observacion,
                    autor);
                await _antecedentes.GuardarRevisionAsync(
                    solicitud,
                    RegistrarCambio(
                        solicitud,
                        autor,
                        "Documento.Revision",
                        peticion.Motivo,
                        anteriores,
                        MapearDocumento(documento)),
                    token);
                return new RevisarAntecedenteRespuesta(documento.Id, solicitud.RevisionDatos);
            },
            tokenCancelacion);
    }

    // Implementa IModuloAntecedentesSolicitud.RegistrarCondicionAsync.
    public Task<RegistrarCondicionRespuesta> RegistrarCondicionAsync(
        Guid institucionId,
        Guid solicitudId,
        RegistrarCondicionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        return EjecutarCambioAsync(
            institucionId,
            solicitudId,
            OperacionInscripciones.RegistrarPersonasOSolicitudes,
            async (
                solicitud,
                autor,
                token) =>
            {
                ComprobarRevision(
                    solicitud,
                    peticion.RevisionSolicitud,
                    peticion.Motivo);
                var condicion = new CondicionPrioridad(
                    solicitud.Id,
                    peticion.Tipo,
                    peticion.ValorDeclarado,
                    peticion.EvidenciaReferencia,
                    autor);
                await _antecedentes.AgregarCondicionAsync(
                    solicitud,
                    condicion,
                    RegistrarCambio(
                        solicitud,
                        autor,
                        "Condicion.Registro",
                        peticion.Motivo,
                        null,
                        MapearCondicion(condicion)),
                    token);
                return new RegistrarCondicionRespuesta(condicion.Id, solicitud.RevisionDatos);
            },
            tokenCancelacion);
    }

    // Implementa IModuloAntecedentesSolicitud.RevisarCondicionAsync: no cambia el valor declarado ni confirma ingresos.
    public Task<RevisarAntecedenteRespuesta> RevisarCondicionAsync(
        Guid institucionId,
        Guid solicitudId,
        Guid condicionId,
        RevisarCondicionPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        return EjecutarCambioAsync(
            institucionId,
            solicitudId,
            OperacionInscripciones.VerificarCondiciones,
            async (
                solicitud,
                autor,
                token) =>
            {
                ComprobarRevision(
                    solicitud,
                    peticion.RevisionSolicitud,
                    peticion.Motivo);
                var condicion = solicitud.Condiciones.SingleOrDefault(c => c.Id == condicionId)
                    ?? throw new KeyNotFoundException("No se encontró la condición en esta solicitud.");
                var anteriores = MapearCondicion(condicion);
                condicion.Revisar(
                    LeerEstado<EstadoVerificacionCondicion>(peticion.Estado),
                    peticion.EvidenciaReferencia,
                    peticion.Observacion,
                    autor);
                await _antecedentes.GuardarRevisionAsync(
                    solicitud,
                    RegistrarCambio(
                        solicitud,
                        autor,
                        "Condicion.Revision",
                        peticion.Motivo,
                        anteriores,
                        MapearCondicion(condicion)),
                    token);
                return new RevisarAntecedenteRespuesta(condicion.Id, solicitud.RevisionDatos);
            },
            tokenCancelacion);
    }

    private async Task<T> EjecutarCambioAsync<T>(
        Guid institucionId,
        Guid solicitudId,
        OperacionInscripciones operacion,
        Func<SolicitudInscripcion, Guid, CancellationToken, Task<T>> cambio,
        CancellationToken tokenCancelacion)
    {
        var autor = await _acceso.AutorizarOperacionAsync(
            institucionId,
            operacion,
            tokenCancelacion);
        var solicitud = await ObtenerSolicitudAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        return await _periodo.EjecutarRevisionAsync(
            institucionId,
            solicitud.PeriodoId,
            async (estado, token) =>
            {
                // Agregar una declaración tras el cierre requiere el permiso específico de corrección.
                // La revisión de declaraciones existentes y la recepción de documentación pueden continuar.
                if (estado == "RecepcionCerrada" && operacion == OperacionInscripciones.RegistrarPersonasOSolicitudes)
                    await _acceso.AutorizarOperacionAsync(
                        institucionId,
                        OperacionInscripciones.CorregirDespuesDelCierre,
                        token);
                return await cambio(
                    solicitud,
                    autor,
                    token);
            },
            tokenCancelacion);
    }

    private async Task<SolicitudInscripcion> ObtenerSolicitudAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
        => await _antecedentes.ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró la solicitud en esta institución.");

    private static void ComprobarRevision(
        SolicitudInscripcion solicitud,
        int revision,
        string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (revision != solicitud.RevisionDatos) throw new ConflictoRevisionSolicitudException();
    }

    private static T LeerEstado<T>(string estado) where T : struct, Enum
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(estado);
        var nombre = Enum.GetNames<T>().SingleOrDefault(n => string.Equals(
            n,
            estado.Trim(),
            StringComparison.OrdinalIgnoreCase));
        return nombre is null ? throw new ArgumentException("El estado indicado no es válido.") : Enum.Parse<T>(nombre);
    }

    private static CambioInscripciones RegistrarCambio(
        SolicitudInscripcion solicitud,
        Guid autor,
        string accion,
        string motivo,
        object? anteriores,
        object nuevos)
    {
        var revisionAnterior = solicitud.RevisionDatos;
        solicitud.RegistrarRevisionAntecedentes();
        return new CambioInscripciones(
            solicitud.InstitucionId,
            autor,
            solicitud.Id,
            accion,
            JsonSerializer.Serialize(new
            {
                SolicitudId = solicitud.Id,
                RevisionAnterior = revisionAnterior,
                RevisionActual = solicitud.RevisionDatos,
                Motivo = motivo.Trim(),
                Anteriores = anteriores,
                Nuevos = nuevos
            }));
    }

    private static DatosDocumentoSolicitud MapearDocumento(DocumentacionSolicitud documento) => new(
        documento.Id,
        documento.Tipo,
        documento.Estado.ToString(),
        documento.Referencia,
        documento.Observacion,
        documento.UsuarioRegistroId,
        documento.FechaRegistro,
        documento.UsuarioRevisionId,
        documento.FechaRevision);

    private static DatosCondicionSolicitud MapearCondicion(CondicionPrioridad condicion) => new(
        condicion.Id,
        condicion.Tipo,
        condicion.ValorDeclarado,
        condicion.EstadoVerificacion.ToString(),
        condicion.EvidenciaReferencia,
        condicion.Observacion,
        condicion.UsuarioRegistroId,
        condicion.FechaRegistro,
        condicion.UsuarioRevisionId,
        condicion.FechaRevision);
}
