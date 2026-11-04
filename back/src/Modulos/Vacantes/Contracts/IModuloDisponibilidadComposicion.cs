namespace Jardin.Vacantes.Contracts;

// Coordina el cambio de composición con las bajas, reservas e ingresos del período.
public interface IModuloDisponibilidadComposicion
{
    Task<T> ConPeriodoProtegidoAsync<T>(
        Guid periodoId,
        Func<DatosDisponibilidadComposicion, CancellationToken, Task<T>> operacion,
        CancellationToken tokenCancelacion = default);
}

public sealed record DatosOcupacionComposicion(
    Guid OfertaId,
    int Edad);

public sealed record DatosOfertaComposicion(
    Guid Id,
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    bool Disponible,
    int CantidadInicial,
    IReadOnlyList<int> EdadesIniciales);

public sealed record DatosDisponibilidadComposicion(
    IReadOnlyList<DatosOfertaComposicion> Ofertas,
    IReadOnlyList<DatosOcupacionComposicion> Ocupaciones,
    IReadOnlyList<DatosIngresoConfirmado> Ingresos);
