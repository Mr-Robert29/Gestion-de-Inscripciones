using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Contracts;

namespace Jardin.Inscripciones.Application.Servicios;

public sealed class ServicioParticipantesSorteo : IModuloParticipantesSorteo
{
    private readonly IRepositorioSolicitudes _solicitudes;
    private readonly IAccesoInstitucionesInscripciones _acceso;

    public ServicioParticipantesSorteo(
        IRepositorioSolicitudes solicitudes,
        IAccesoInstitucionesInscripciones acceso)
    {
        _solicitudes = solicitudes;
        _acceso = acceso;
    }

    public async Task<IReadOnlyList<DatosParticipanteSorteo>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        int sala,
        CancellationToken tokenCancelacion = default)
    {
        await _acceso.AutorizarAsync(institucionId, false, tokenCancelacion);
        var participantes = await _solicitudes.ListarParticipantesSorteoAsync(
            institucionId,
            periodoId,
            sala,
            tokenCancelacion);
        return participantes
            .Select(x => new DatosParticipanteSorteo(
                x.SolicitudId,
                x.AspiranteId,
                institucionId,
                periodoId,
                x.Sala,
                x.Dni))
            .ToArray();
    }
}
