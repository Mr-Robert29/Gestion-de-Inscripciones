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
    private readonly IAutorizacionComposicion _autorizacion;

    public ServicioOrganizacion(
        IRepositorioSecciones repositorio,
        IRepositorioDistribuciones repositorioDistribuciones,
        IProveedorOfertaSecciones oferta,
        IModuloVacantes vacantes,
        IModuloComposicionSecciones composicion,
        IAutorizacionComposicion autorizacion)
    {
        _repositorio = repositorio;
        _repositorioDistribuciones = repositorioDistribuciones;
        _oferta = oferta;
        _vacantes = vacantes;
        _composicion = composicion;
        _autorizacion = autorizacion;
    }

    public async Task<Guid> DefinirSeccionAsync(
        DefinirSeccionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        await _autorizacion.AutorizarCambioAsync(peticion.PeriodoId, cancellationToken);
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
        if (seccion is null) return null;
        await _autorizacion.AutorizarConsultaHistorialAsync(seccion.PeriodoId, cancellationToken);
        return Mapear(seccion);
    }

    public async Task<IReadOnlyList<DatosSeccion>> ListarSeccionesPorPeriodoAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacion.AutorizarConsultaHistorialAsync(periodoId, cancellationToken);
        var secciones = await _repositorio.ListarPorPeriodoAsync(periodoId, cancellationToken);
        return secciones.Select(Mapear).ToList();
    }

    public async Task<IReadOnlyList<DatosAsignacionSeccionActiva>> ListarAsignacionesActivasAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacion.AutorizarConsultaHistorialAsync(periodoId, cancellationToken);
        var asignaciones = await _repositorioDistribuciones.ListarAsignacionesActivasAsync(periodoId, cancellationToken);
        if (asignaciones.Count == 0) return [];

        var ingresos = (await _vacantes.ListarIngresosAsync(periodoId, cancellationToken))
            .Where(x => x.Activo && !x.BajaEnProceso)
            .ToDictionary(x => x.Id);
        var secciones = (await _repositorio.ListarPorPeriodoAsync(periodoId, cancellationToken))
            .ToDictionary(x => x.Id);

        return asignaciones
            .Where(x => ingresos.ContainsKey(x.IngresoId) && secciones.ContainsKey(x.SeccionId))
            .Select(x =>
            {
                var ingreso = ingresos[x.IngresoId];
                var seccion = secciones[x.SeccionId];
                return new DatosAsignacionSeccionActiva(
                    ingreso.Id,
                    ingreso.SolicitudId,
                    ingreso.Edad,
                    ingreso.Sala,
                    ingreso.TurnoId,
                    seccion.Nombre);
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<DatosPropuestaDistribucion>> ListarPropuestasDistribucionAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacion.AutorizarConsultaHistorialAsync(periodoId, cancellationToken);
        return (await _repositorioDistribuciones.ListarPorPeriodoAsync(periodoId, cancellationToken)).Select(x => Mapear(x)).ToArray();
    }

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

        usuarioId = await _autorizacion.AutorizarDistribucionAsync(
            periodoId,
            false,
            cancellationToken);
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

        var asignacionesActivas = await _repositorioDistribuciones.ListarAsignacionesActivasAsync(periodoId, cancellationToken);
        var ingresosConAsignacion = asignacionesActivas.Select(x => x.IngresoId).ToHashSet();
        var ocupacion = asignacionesActivas
            .GroupBy(x => x.SeccionId)
            .ToDictionary(x => x.Key, x => x.Count());
        var ingresosPendientes = ingresos.Ingresos
            .Where(x => x.Activo && !ingresosConAsignacion.Contains(x.IngresoId))
            .ToArray();
        var asignacionesOptimizadas = AsignarIngresosALasSecciones(ingresosPendientes, secciones, ocupacion);
        foreach (var ingreso in ingresosPendientes)
        {
            if (!asignacionesOptimizadas.TryGetValue(ingreso.IngresoId, out var seccionId))
            {
                var tieneSeccionCompatible = secciones.Any(x => x.EsCompatibleCon(ingreso.Edad, ingreso.TurnoId));
                propuesta.AgregarAsignacion(new AsignacionPropuesta(
                    ingreso.IngresoId,
                    ingreso.Edad,
                    ingreso.TurnoId,
                    null,
                    tieneSeccionCompatible
                        ? "La capacidad de las secciones compatibles no alcanza para todos los ingresos de esta distribución."
                        : "No existe una sección activa que admita la edad y el turno de este ingreso."));
                continue;
            }

            propuesta.AgregarAsignacion(new AsignacionPropuesta(
                ingreso.IngresoId,
                ingreso.Edad,
                ingreso.TurnoId,
                seccionId,
                null));
        }

        await _repositorioDistribuciones.AgregarAsync(propuesta, cancellationToken);
        return await MapearConSolicitudesAsync(propuesta, cancellationToken);
    }

    // Asigna primero a los ingresos con menos secciones compatibles y usa caminos de reasignación
    // para maximizar la cantidad ubicada sin consumir una sección multiedad con quien tiene opciones puras.
    private static IReadOnlyDictionary<Guid, Guid> AsignarIngresosALasSecciones(
        IReadOnlyList<DatosIngresoConfirmado> ingresos,
        IReadOnlyList<Seccion> secciones,
        IReadOnlyDictionary<Guid, int> ocupacionActual)
    {
        var plazasPorSeccion = secciones
            .Where(x => x.Activa)
            .SelectMany(seccion => Enumerable.Range(
                    0,
                    Math.Max(0, seccion.Capacidad - ocupacionActual.GetValueOrDefault(seccion.Id)))
                .Select(numero => new PlazaSeccion(seccion.Id, numero)))
            .GroupBy(x => x.SeccionId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var plazasPorIngreso = ingresos.ToDictionary(
            ingreso => ingreso.IngresoId,
            ingreso => secciones
                .Where(seccion => seccion.EsCompatibleCon(ingreso.Edad, ingreso.TurnoId) &&
                    plazasPorSeccion.ContainsKey(seccion.Id))
                .OrderBy(seccion => seccion.EdadesAdmitidas.Count)
                .ThenBy(seccion => seccion.Nombre, StringComparer.OrdinalIgnoreCase)
                .ThenBy(seccion => seccion.Id)
                .SelectMany(seccion => plazasPorSeccion[seccion.Id])
                .ToArray());

        var ocupantePorPlaza = new Dictionary<PlazaSeccion, Guid>();
        bool BuscarPlaza(Guid ingresoId, HashSet<PlazaSeccion> visitadas)
        {
            foreach (var plaza in plazasPorIngreso[ingresoId])
            {
                if (!visitadas.Add(plaza)) continue;
                if (!ocupantePorPlaza.TryGetValue(plaza, out var ocupante) || BuscarPlaza(ocupante, visitadas))
                {
                    ocupantePorPlaza[plaza] = ingresoId;
                    return true;
                }
            }
            return false;
        }

        var ordenOriginal = ingresos
            .Select((ingreso, indice) => (ingreso.IngresoId, indice))
            .ToDictionary(x => x.IngresoId, x => x.indice);
        foreach (var ingreso in ingresos
                     .OrderBy(x => plazasPorIngreso[x.IngresoId].Select(p => p.SeccionId).Distinct().Count())
                     .ThenBy(x => plazasPorIngreso[x.IngresoId].Length)
                     .ThenBy(x => ordenOriginal[x.IngresoId]))
        {
            BuscarPlaza(ingreso.IngresoId, new HashSet<PlazaSeccion>());
        }

        return ocupantePorPlaza
            .Select(x => (x.Value, x.Key.SeccionId))
            .GroupBy(x => x.Value)
            .ToDictionary(x => x.Key, x => x.First().SeccionId);
    }

    private readonly record struct PlazaSeccion(Guid SeccionId, int Numero);

    public async Task<DatosPropuestaDistribucion?> ObtenerPropuestaDistribucionAsync(
        Guid propuestaId,
        CancellationToken cancellationToken = default)
    {
        var propuesta = await _repositorioDistribuciones.ObtenerPorIdAsync(propuestaId, cancellationToken);
        if (propuesta is null) return null;
        await _autorizacion.AutorizarConsultaHistorialAsync(propuesta.PeriodoId, cancellationToken);
        return await MapearConSolicitudesAsync(propuesta, cancellationToken);
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

        usuarioId = await _autorizacion.AutorizarDistribucionAsync(
            propuesta.PeriodoId,
            true,
            cancellationToken);
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
        return await MapearConSolicitudesAsync(propuesta, cancellationToken);
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
        usuarioId = await _autorizacion.AutorizarDistribucionAsync(
            propuesta.PeriodoId,
            true,
            cancellationToken);
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
        return await MapearConSolicitudesAsync(propuesta, cancellationToken);
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

    private async Task<DatosPropuestaDistribucion> MapearConSolicitudesAsync(
        PropuestaDistribucion propuesta,
        CancellationToken cancellationToken)
    {
        var solicitudesPorIngreso = (await _vacantes.ListarIngresosAsync(propuesta.PeriodoId, cancellationToken))
            .ToDictionary(x => x.Id, x => x.SolicitudId);
        return Mapear(propuesta, solicitudesPorIngreso);
    }

    private static DatosPropuestaDistribucion Mapear(
        PropuestaDistribucion propuesta,
        IReadOnlyDictionary<Guid, Guid>? solicitudesPorIngreso = null) => new(
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
            x.MotivoModificacion,
            solicitudesPorIngreso?.GetValueOrDefault(x.IngresoId))).ToList());
}
