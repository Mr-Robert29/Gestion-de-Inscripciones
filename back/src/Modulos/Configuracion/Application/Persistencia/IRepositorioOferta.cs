using Jardin.Configuracion.Domain.Entidades;

namespace Jardin.Configuracion.Application.Persistencia;

// Cada escritura conserva los datos y su historial en una sola transacción.
public interface IRepositorioOferta
{
    Task<Aula?> ObtenerAulaAsync(Guid id, CancellationToken tokenCancelacion = default);
    Task<Turno?> ObtenerTurnoAsync(Guid id, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<Aula>> ListarAulasAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<Turno>> ListarTurnosAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
    Task<OfertaAulaTurno?> ObtenerOfertaAsync(Guid id, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<OfertaAulaTurno>> ListarOfertasAsync(Guid periodoId, CancellationToken tokenCancelacion = default);
    Task<bool> ExisteCombinacionAsync(
        Guid periodoId,
        Guid aulaId,
        Guid turnoId,
        CancellationToken tokenCancelacion = default);
    Task AgregarAulaAsync(
        Aula aula,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    Task AgregarTurnoAsync(
        Turno turno,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    Task AgregarOfertaAsync(
        OfertaAulaTurno oferta,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
    Task ActualizarOfertaAsync(
        OfertaAulaTurno oferta,
        CambioInstitucion cambio,
        CancellationToken tokenCancelacion = default);
}
