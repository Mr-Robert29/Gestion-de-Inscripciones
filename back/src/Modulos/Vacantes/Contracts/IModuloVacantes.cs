namespace Jardin.Vacantes.Contracts;

public interface IModuloVacantes
{
    Task<RevisionIngresosActivos> ObtenerIngresosActivosAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosIngresoHistorial>> ListarIngresosAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);
}

public sealed record RevisionIngresosActivos(
    int Revision,
    IReadOnlyList<DatosIngresoConfirmado> Ingresos);

public sealed record DatosIngresoConfirmado(
    Guid IngresoId,
    Guid AspiranteId,
    int Edad,
    Guid TurnoId,
    bool Activo);
