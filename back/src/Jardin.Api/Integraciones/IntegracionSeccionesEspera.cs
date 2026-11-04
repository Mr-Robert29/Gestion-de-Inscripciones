using Jardin.Secciones.Contracts;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Api.Integraciones;

public sealed class IntegracionSeccionesEspera : IIntegracionSeccionesEspera
{
    private readonly IModuloSeccionesEspera _secciones;
    public IntegracionSeccionesEspera(IModuloSeccionesEspera secciones) { _secciones = secciones; }

    // Implementa IIntegracionSeccionesEspera.DarBajaAsignacionesAsync por el contrato público de Secciones.
    public Task DarBajaAsignacionesAsync(
        Guid periodoId,
        Guid ingresoId,
        Guid usuarioId,
        string motivo,
        CancellationToken tokenCancelacion = default)
        => _secciones.DarBajaAsignacionesAsync(periodoId, ingresoId, usuarioId, motivo, tokenCancelacion);

    // Implementa IIntegracionSeccionesEspera.ConOfertaProtegidaAsync conservando el bloqueo durante la operación.
    public Task<T> ConOfertaProtegidaAsync<T>(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        Func<IReadOnlyList<ComposicionVacante>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => _secciones.ConOfertaProtegidaAsync(
            periodoId,
            aulaId,
            turnoId,
            (secciones, token) => operacion(secciones.Select(x => new ComposicionVacante(x.Id, x.EdadesAdmitidas, x.Capacidad, x.Revision)).ToArray(), token),
            tokenCancelacion);
}
