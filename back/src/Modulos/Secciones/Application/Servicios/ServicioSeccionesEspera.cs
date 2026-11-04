using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Domain.Entidades;

namespace Jardin.Secciones.Application.Servicios;

public sealed class ServicioSeccionesEspera : IModuloSeccionesEspera
{
    private readonly IRepositorioSeccionesEspera _repositorio;
    public ServicioSeccionesEspera(IRepositorioSeccionesEspera repositorio) { _repositorio = repositorio; }

    // Implementa IModuloSeccionesEspera.DarBajaAsignacionesAsync de manera reintentable.
    public Task DarBajaAsignacionesAsync(
        Guid periodoId,
        Guid ingresoId,
        Guid usuarioId,
        string motivo,
        CancellationToken tokenCancelacion = default)
        => _repositorio.DarBajaAsync(new IngresoRetiradoSecciones(ingresoId, periodoId, usuarioId, motivo), tokenCancelacion);

    // Implementa IModuloSeccionesEspera.ConOfertaProtegidaAsync: ofrece datos públicos bajo el bloqueo de composición.
    public Task<T> ConOfertaProtegidaAsync<T>(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        Func<IReadOnlyList<DatosSeccion>, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default)
        => _repositorio.ConOfertaProtegidaAsync(
            periodoId,
            aulaId,
            turnoId,
            (secciones, token) => operacion(secciones.Select(x => new DatosSeccion(
                x.Id, x.PeriodoId, x.AulaId, x.TurnoId, x.Nombre, x.Tipo.ToString(), x.EdadesAdmitidas.ToArray(), x.Capacidad, x.Revision, x.Activa)).ToArray(), token),
            tokenCancelacion);
}
