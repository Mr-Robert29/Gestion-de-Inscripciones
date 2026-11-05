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

    // Implementa IModuloVacantes.ObtenerIngresosActivosAsync: la revisión incluye cambios de pertenencia y bajas.
    public async Task<RevisionIngresosActivos> ObtenerIngresosActivosAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var ingresos = await _repositorio.ListarIngresosAsync(periodoId, cancellationToken);
        var revision = checked((int)ingresos.Sum(x => (long)x.Revision));
        return new RevisionIngresosActivos(
            revision,
            ingresos.Where(x => x.Activo && !x.BajaEnProceso).Select(x => new DatosIngresoConfirmado(
                x.Id,
                x.AspiranteId,
                x.Edad,
                x.TurnoId,
                true)).ToArray());
    }

    public async Task<IReadOnlyList<DatosIngresoHistorial>> ListarIngresosAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
        => (await _repositorio.ListarIngresosAsync(periodoId, cancellationToken))
            .Select(x => new DatosIngresoHistorial(
                x.Id, x.PeriodoId, x.SolicitudId, x.AspiranteId, x.Edad,
                x.OfertaId, x.TurnoId, x.Sala, x.PropuestaId, x.UsuarioId,
                x.Fecha, x.Revision, x.Activo, x.BajaEnProceso, x.UsuarioBajaId,
                x.FechaSolicitudBaja, x.FechaBaja, x.MotivoBaja, x.OfrecimientoId))
            .ToArray();
}
