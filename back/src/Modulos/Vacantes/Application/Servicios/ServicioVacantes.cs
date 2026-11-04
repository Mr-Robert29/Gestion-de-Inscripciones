using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Contracts;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioVacantes : IModuloVacantes
{
    private readonly IRepositorioPropuestasIngresos _repositorio;

    public ServicioVacantes(IRepositorioPropuestasIngresos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<RevisionIngresosActivos> ObtenerIngresosActivosAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var ingresos = await _repositorio.ListarIngresosActivosAsync(periodoId, cancellationToken);
        var revision = ingresos.Count == 0 ? 0 : ingresos.Max(x => x.Revision);
        return new RevisionIngresosActivos(
            revision,
            ingresos.Select(x => new DatosIngresoConfirmado(
                x.Id,
                x.AspiranteId,
                x.Edad,
                x.TurnoId,
                true)).ToArray());
    }
}
