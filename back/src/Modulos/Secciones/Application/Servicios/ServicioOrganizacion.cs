using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Domain.Entidades;
using Jardin.Vacantes.Contracts;

namespace Jardin.Secciones.Application.Servicios;

public sealed class ServicioOrganizacion : IModuloSecciones
{
    private readonly IRepositorioSecciones _repositorio;
    private readonly IRepositorioDistribuciones _repositorioDistribuciones;
    private readonly IProveedorOfertaSecciones _oferta;
    private readonly IModuloVacantes _vacantes;

    public ServicioOrganizacion(
        IRepositorioSecciones repositorio,
        IRepositorioDistribuciones repositorioDistribuciones,
        IProveedorOfertaSecciones oferta,
        IModuloVacantes vacantes)
    {
        _repositorio = repositorio;
        _repositorioDistribuciones = repositorioDistribuciones;
        _oferta = oferta;
        _vacantes = vacantes;
    }

    public async Task<Guid> DefinirSeccionAsync(
        DefinirSeccionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var oferta = await _oferta.ObtenerAsync(
            peticion.PeriodoId,
            peticion.AulaId,
            peticion.TurnoId,
            cancellationToken);

        if (oferta is null)
        {
            throw new InvalidOperationException(
                "El aula y el turno no pertenecen a la oferta configurada para el período.");
        }

        var seccion = new Seccion(
            oferta.PeriodoId,
            oferta.AulaId,
            oferta.TurnoId,
            peticion.Nombre,
            peticion.EdadesAdmitidas,
            oferta.Capacidad,
            oferta.CombinacionesPermitidas);

        await _repositorio.AgregarAsync(seccion, cancellationToken);
        return seccion.Id;
    }

    public async Task<DatosSeccion?> ObtenerSeccionPorIdAsync(
        Guid seccionId,
        CancellationToken cancellationToken = default)
    {
        var seccion = await _repositorio.ObtenerPorIdAsync(seccionId, cancellationToken);
        return seccion is null ? null : Mapear(seccion);
    }

    public async Task<IReadOnlyList<DatosSeccion>> ListarSeccionesPorPeriodoAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        var secciones = await _repositorio.ListarPorPeriodoAsync(periodoId, cancellationToken);
        return secciones.Select(Mapear).ToList();
    }

    public async Task<bool> CambiarComposicionSeccionAsync(
        Guid seccionId,
        CambiarComposicionSeccionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var seccion = await _repositorio.ObtenerPorIdAsync(seccionId, cancellationToken);
        if (seccion is null)
            return false;

        var oferta = await _oferta.ObtenerAsync(
            seccion.PeriodoId,
            seccion.AulaId,
            seccion.TurnoId,
            cancellationToken);

        if (oferta is null)
            throw new InvalidOperationException("La oferta del período ya no está disponible.");
        seccion.CambiarComposicion(
            peticion.NuevasEdades,
            peticion.Motivo,
            oferta.CombinacionesPermitidas,
            edadesAlumnosAsignados: null);

        await _repositorio.ActualizarAsync(seccion, cancellationToken);
        return true;
    }

    public async Task<DatosPropuestaDistribucion> GenerarDistribucionAsync(
        Guid periodoId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (periodoId == Guid.Empty)
            throw new ArgumentException("El período no puede estar vacío.", nameof(periodoId));
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario no puede estar vacío.", nameof(usuarioId));

        var ingresos = await _vacantes.ObtenerIngresosActivosAsync(periodoId, cancellationToken);
        var secciones = (await _repositorio.ListarPorPeriodoAsync(periodoId, cancellationToken)).ToList();
        if (secciones.Count == 0)
            throw new InvalidOperationException("El período no tiene secciones activas definidas.");

        var version = await _repositorioDistribuciones.ObtenerUltimaVersionAsync(periodoId, cancellationToken) + 1;
        var propuesta = new PropuestaDistribucion(
            periodoId,
            version,
            ingresos.Revision,
            secciones.Max(x => x.Revision),
            usuarioId,
            DateTimeOffset.UtcNow);

        var ocupacion = secciones.ToDictionary(x => x.Id, _ => 0);
        foreach (var ingreso in ingresos.Ingresos.Where(x => x.Activo))
        {
            var seccion = secciones.FirstOrDefault(x =>
                ocupacion[x.Id] < x.Capacidad &&
                x.EsCompatibleCon(ingreso.Edad, ingreso.TurnoId));

            if (seccion is null)
            {
                propuesta.AgregarAsignacion(new AsignacionPropuesta(
                    ingreso.IngresoId,
                    ingreso.Edad,
                    ingreso.TurnoId,
                    null,
                    "No existe una sección activa compatible con la edad, el turno y la capacidad disponible."));
                continue;
            }

            ocupacion[seccion.Id]++;
            propuesta.AgregarAsignacion(new AsignacionPropuesta(
                ingreso.IngresoId,
                ingreso.Edad,
                ingreso.TurnoId,
                seccion.Id,
                null));
        }

        await _repositorioDistribuciones.AgregarAsync(propuesta, cancellationToken);
        return Mapear(propuesta);
    }

    public async Task<DatosPropuestaDistribucion?> ObtenerPropuestaDistribucionAsync(
        Guid propuestaId,
        CancellationToken cancellationToken = default)
    {
        var propuesta = await _repositorioDistribuciones.ObtenerPorIdAsync(propuestaId, cancellationToken);
        return propuesta is null ? null : Mapear(propuesta);
    }

    public Task<DatosPropuestaDistribucion> ModificarAsignacionAsync(
        Guid propuestaId,
        ModificarAsignacionPropuestaPeticion peticion,
        Guid usuarioId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<DatosPropuestaDistribucion?> ConfirmarDistribucionAsync(
        Guid propuestaId,
        Guid usuarioId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    private static DatosSeccion Mapear(Seccion seccion) => new(
        seccion.Id,
        seccion.PeriodoId,
        seccion.AulaId,
        seccion.TurnoId,
        seccion.Nombre,
        seccion.Tipo.ToString(),
        seccion.EdadesAdmitidas.ToList(),
        seccion.Capacidad,
        seccion.Revision,
        seccion.Activa);

    private static DatosPropuestaDistribucion Mapear(PropuestaDistribucion propuesta) => new(
        propuesta.Id,
        propuesta.PeriodoId,
        propuesta.Version,
        propuesta.RevisionIngresos,
        propuesta.RevisionSecciones,
        propuesta.Estado.ToString(),
        propuesta.FechaCreacion,
        propuesta.CreadaPor,
        propuesta.ConfirmadaPor,
        propuesta.FechaConfirmacion,
        propuesta.Asignaciones.Select(x => new DatosAsignacionPropuesta(
            x.Id,
            x.IngresoId,
            x.SeccionId,
            x.Edad,
            x.TurnoId,
            x.MotivoSinSeccion)).ToList());
}
