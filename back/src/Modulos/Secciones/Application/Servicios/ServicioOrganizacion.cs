using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Domain;
using Jardin.Secciones.Domain.Entidades;
using Jardin.Vacantes.Contracts;

namespace Jardin.Secciones.Application.Servicios;

public sealed class ServicioOrganizacion : IModuloSecciones
{
    private readonly IRepositorioSecciones _repositorio;
    private readonly IRepositorioDistribuciones _repositorioDistribuciones;
    private readonly IProveedorOfertaSecciones _oferta;
    private readonly IModuloVacantes _vacantes;
    private readonly IModuloComposicionSecciones _composicion;

    public ServicioOrganizacion(
        IRepositorioSecciones repositorio,
        IRepositorioDistribuciones repositorioDistribuciones,
        IProveedorOfertaSecciones oferta,
        IModuloVacantes vacantes,
        IModuloComposicionSecciones composicion)
    {
        _repositorio = repositorio;
        _repositorioDistribuciones = repositorioDistribuciones;
        _oferta = oferta;
        _vacantes = vacantes;
        _composicion = composicion;
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

    public async Task<IReadOnlyList<DatosPropuestaDistribucion>> ListarPropuestasDistribucionAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
        => (await _repositorioDistribuciones.ListarPorPeriodoAsync(periodoId, cancellationToken))
            .Select(Mapear)
            .ToArray();

    // Implementa IModuloSecciones.CambiarComposicionSeccionAsync mediante la operación protegida de HU-26.
    public async Task<bool> CambiarComposicionSeccionAsync(
        Guid seccionId,
        CambiarComposicionSeccionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var seccion = await _repositorio.ObtenerPorIdAsync(seccionId, cancellationToken);
        if (seccion is null)
            return false;

        await _composicion.CambiarAsync(
            seccion.PeriodoId,
            seccionId,
            peticion,
            cancellationToken);
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
            secciones.Sum(x => x.Revision),
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

    public async Task<DatosPropuestaDistribucion> ModificarAsignacionAsync(
        Guid propuestaId,
        ModificarAsignacionPropuestaPeticion peticion,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario no puede estar vacío.", nameof(usuarioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);

        var propuesta = await _repositorioDistribuciones.ObtenerPorIdAsync(propuestaId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró la propuesta de distribución.");

        if (propuesta.Estado != EstadoPropuestaDistribucion.Borrador)
            throw new InvalidOperationException("Solo se puede modificar una propuesta vigente.");

        var asignacion = propuesta.Asignaciones.FirstOrDefault(x => x.IngresoId == peticion.IngresoId)
            ?? throw new InvalidOperationException("El ingreso no forma parte de la propuesta.");

        if (peticion.SeccionId is not null)
        {
            var seccion = await _repositorio.ObtenerPorIdAsync(peticion.SeccionId.Value, cancellationToken)
                ?? throw new KeyNotFoundException("No se encontró la sección indicada.");

            if (seccion.PeriodoId != propuesta.PeriodoId || !seccion.EsCompatibleCon(asignacion.Edad, asignacion.TurnoId))
                throw new InvalidOperationException("La sección no es compatible con la edad, el turno o el período del ingreso.");

            var ocupacion = propuesta.Asignaciones.Count(x => x.SeccionId == seccion.Id && x.IngresoId != peticion.IngresoId);
            if (ocupacion >= seccion.Capacidad)
                throw new InvalidOperationException("La sección no tiene capacidad disponible.");
        }

        propuesta.ModificarAsignacion(
            peticion.IngresoId,
            peticion.SeccionId,
            peticion.MotivoSinSeccion,
            usuarioId,
            DateTimeOffset.UtcNow,
            peticion.Motivo);

        await _repositorioDistribuciones.ActualizarAsync(propuesta, cancellationToken);
        return Mapear(propuesta);
    }

    public Task<DatosPropuestaDistribucion?> ConfirmarDistribucionAsync(
        Guid propuestaId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return ConfirmarDistribucionInternaAsync(propuestaId, usuarioId, cancellationToken);
    }

    private async Task<DatosPropuestaDistribucion?> ConfirmarDistribucionInternaAsync(
        Guid propuestaId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario no puede estar vacío.", nameof(usuarioId));

        var propuesta = await _repositorioDistribuciones.ObtenerPorIdAsync(propuestaId, cancellationToken);
        if (propuesta is null)
            return null;
        if (propuesta.Estado != EstadoPropuestaDistribucion.Borrador)
            throw new InvalidOperationException("Solo se puede confirmar una propuesta vigente.");

        var ingresos = await _vacantes.ObtenerIngresosActivosAsync(propuesta.PeriodoId, cancellationToken);
        if (ingresos.Revision != propuesta.RevisionIngresos)
        {
            propuesta.MarcarDesactualizada();
            await _repositorioDistribuciones.ActualizarAsync(propuesta, cancellationToken);
            throw new InvalidOperationException("La propuesta está desactualizada porque cambiaron los ingresos.");
        }

        var secciones = (await _repositorio.ListarPorPeriodoAsync(propuesta.PeriodoId, cancellationToken)).ToList();
        if (secciones.Count == 0 || secciones.Sum(x => x.Revision) != propuesta.RevisionSecciones)
        {
            propuesta.MarcarDesactualizada();
            await _repositorioDistribuciones.ActualizarAsync(propuesta, cancellationToken);
            throw new InvalidOperationException("La propuesta está desactualizada porque cambiaron las secciones.");
        }

        var asignaciones = new List<AsignacionSeccion>();
        foreach (var propuestaAsignacion in propuesta.Asignaciones.Where(x => x.SeccionId is not null))
        {
            var seccion = secciones.FirstOrDefault(x => x.Id == propuestaAsignacion.SeccionId);
            if (seccion is null || !seccion.EsCompatibleCon(propuestaAsignacion.Edad, propuestaAsignacion.TurnoId))
                throw new InvalidOperationException("La propuesta contiene una asignación incompatible.");
            if (await _repositorioDistribuciones.ExisteAsignacionActivaAsync(propuestaAsignacion.IngresoId, cancellationToken))
                throw new InvalidOperationException("Uno de los ingresos ya tiene una asignación activa.");
            if (asignaciones.Count(x => x.SeccionId == seccion.Id) >= seccion.Capacidad)
                throw new InvalidOperationException("La propuesta supera la capacidad de una sección.");

            asignaciones.Add(new AsignacionSeccion(
                propuestaAsignacion.IngresoId,
                seccion.Id,
                usuarioId,
                DateTimeOffset.UtcNow,
                propuestaAsignacion.MotivoModificacion));
        }

        propuesta.Confirmar(usuarioId, DateTimeOffset.UtcNow);
        await _repositorioDistribuciones.ConfirmarAsync(propuesta, asignaciones, cancellationToken);
        return Mapear(propuesta);
    }

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
            x.MotivoSinSeccion,
            x.ModificadaPor,
            x.ModificadaEn,
            x.MotivoModificacion)).ToList());
}
