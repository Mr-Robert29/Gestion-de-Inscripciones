namespace Jardin.Vacantes.Contracts;

public interface IModuloSorteos
{
    Task<DatosSorteo> RegistrarAsync(
        Guid periodoId,
        RegistrarSorteoPeticion peticion,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatosSorteo>> ListarVigentesAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatosOrdenSorteo>> CalcularOrdenAsync(
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default);

}
