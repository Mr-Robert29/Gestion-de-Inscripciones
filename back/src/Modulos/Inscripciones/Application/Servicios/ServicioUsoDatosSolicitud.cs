using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioUsoDatosSolicitud : IModuloUsoDatosSolicitud
{
    private readonly IRepositorioUsoDatosSolicitud _usos;
    private readonly IRepositorioSolicitudes _solicitudes;
    private readonly IAccesoInstitucionesInscripciones _acceso;
    public ServicioUsoDatosSolicitud(
        IRepositorioUsoDatosSolicitud usos,
        IRepositorioSolicitudes solicitudes,
        IAccesoInstitucionesInscripciones acceso)
    {
        _usos = usos;
        _solicitudes = solicitudes;
        _acceso = acceso;
    }

    // Implementa IModuloUsoDatosSolicitud.RegistrarUsoAsync: solo registra las entradas que informa el consumidor.
    public async Task RegistrarUsoAsync(
        Guid institucionId,
        Guid solicitudId,
        DatosUsoSolicitud uso,
        CancellationToken tokenCancelacion = default)
    {
        var solicitud = await ObtenerAutorizadaAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        ArgumentNullException.ThrowIfNull(uso);
        if (uso.RevisionDatos > solicitud.RevisionDatos || uso.RevisionDomicilio > solicitud.RevisionDomicilio)
            throw new ArgumentException("No se pueden registrar revisiones futuras.");
        await _usos.RegistrarAsync(new UsoDatosSolicitud(
            solicitud.Id,
            uso.ReferenciaId,
            uso.Tipo,
            uso.RevisionDatos,
            uso.RevisionDomicilio), tokenCancelacion);
    }

    // Implementa IModuloUsoDatosSolicitud.ConsultarImpactoAsync: no presupone que las referencias estén vigentes en su módulo.
    public async Task<ConsultarImpactoSolicitudRespuesta> ConsultarImpactoAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
    {
        var solicitud = await ObtenerAutorizadaAsync(
            institucionId,
            solicitudId,
            tokenCancelacion);
        var usos = await _usos.ListarAsync(solicitudId, tokenCancelacion);
        return new ConsultarImpactoSolicitudRespuesta(
            solicitud.Id,
            solicitud.RevisionDatos,
            solicitud.RevisionDomicilio,
            usos.Where(u => u.RevisionDatos <= solicitud.RevisionDatos && u.RevisionDomicilio <= solicitud.RevisionDomicilio && u.EstaDesactualizado(solicitud)).Select(u => new DatosUsoSolicitud(
                u.ReferenciaId,
                u.Tipo,
                u.RevisionDatos,
                u.RevisionDomicilio)).ToArray());
    }

    private async Task<SolicitudInscripcion> ObtenerAutorizadaAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion)
    {
        await _acceso.AutorizarOperacionAsync(
            institucionId,
            OperacionInscripciones.Consultar,
            tokenCancelacion);
        return await _solicitudes.ObtenerAsync(
            institucionId,
            solicitudId,
            tokenCancelacion) ?? throw new KeyNotFoundException("No se encontró la solicitud en esta institución.");
    }
}
