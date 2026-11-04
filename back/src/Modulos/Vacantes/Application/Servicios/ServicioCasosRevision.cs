using System.Globalization;
using Jardin.Inscripciones.Contracts;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioCasosRevision : IModuloCasosRevision
{
    private readonly IRepositorioCasosRevision _repositorio;
    private readonly IAutorizacionVacantes _autorizacion;
    private readonly IModuloPrioridades _prioridades;
    private readonly IModuloSorteos _sorteos;
    private readonly IModuloDistancias _distancias;
    private readonly IModuloRevisionDomicilios _revisionesDomicilio;
    private readonly IProveedorReglasPeriodo _reglas;

    public ServicioCasosRevision(
        IRepositorioCasosRevision repositorio,
        IAutorizacionVacantes autorizacion,
        IModuloPrioridades prioridades,
        IModuloSorteos sorteos,
        IModuloDistancias distancias,
        IModuloRevisionDomicilios revisionesDomicilio,
        IProveedorReglasPeriodo reglas)
    {
        _repositorio = repositorio;
        _autorizacion = autorizacion;
        _prioridades = prioridades;
        _sorteos = sorteos;
        _distancias = distancias;
        _revisionesDomicilio = revisionesDomicilio;
        _reglas = reglas;
    }

    public async Task<IReadOnlyList<DatosCasoRevision>> ListarAsync(
        Guid periodoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacion.AutorizarResolucionAsync(periodoId, cancellationToken);
        await GenerarDesdeResultadosAsync(periodoId, cancellationToken);
        return (await _repositorio.ListarAsync(periodoId, cancellationToken)).Select(Mapear).ToArray();
    }

    public async Task<DatosCasoRevision?> ConsultarAsync(
        Guid casoId,
        CancellationToken cancellationToken = default)
    {
        var caso = await _repositorio.ObtenerAsync(casoId, cancellationToken);
        if (caso is null) return null;
        await _autorizacion.AutorizarResolucionAsync(caso.PeriodoId, cancellationToken);
        return Mapear(caso);
    }

    public async Task<IReadOnlyList<DatosHistorialCasoRevision>> ConsultarHistorialAsync(
        Guid casoId,
        CancellationToken cancellationToken = default)
    {
        var caso = await _repositorio.ObtenerAsync(casoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el caso de revisión.");
        await _autorizacion.AutorizarResolucionAsync(caso.PeriodoId, cancellationToken);
        var historial = await _repositorio.ListarHistorialAsync(casoId, cancellationToken);
        return historial.Select(x => new DatosHistorialCasoRevision(
            x.Id,
            x.CasoRevisionId,
            x.Accion,
            x.EstadoAnterior.ToString(),
            x.EstadoNuevo.ToString(),
            System.Text.Json.JsonSerializer.Deserialize<Guid[]>(x.OrdenAnterior) ?? [],
            System.Text.Json.JsonSerializer.Deserialize<Guid[]>(x.OrdenNueva) ?? [],
            x.Motivo,
            x.UsuarioId,
            x.Fecha)).ToArray();
    }

    public async Task<DatosCasoRevision> ResolverAsync(
        Guid casoId,
        ResolverCasoRevisionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var caso = await _repositorio.ObtenerAsync(casoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el caso de revisión.");
        var autorizacion = await _autorizacion.AutorizarResolucionAsync(caso.PeriodoId, cancellationToken);
        var estadoAnterior = caso.Estado;
        var ordenAnterior = caso.ObtenerOrdenDecidido();
        caso.Resolver(autorizacion.UsuarioId, peticion.OrdenDecidido, peticion.Motivo, peticion.Revision);
        await GuardarConHistorialAsync(
            caso,
            new HistorialCasoRevision(
                caso,
                "Resolver",
                autorizacion.UsuarioId,
                peticion.Motivo,
                ordenAnterior,
                peticion.OrdenDecidido,
                estadoAnterior,
                caso.Estado),
            cancellationToken);
        return Mapear(caso);
    }

    public async Task<DatosCasoRevision> ReabrirAsync(
        Guid casoId,
        ReabrirCasoRevisionPeticion peticion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var caso = await _repositorio.ObtenerAsync(casoId, cancellationToken)
            ?? throw new KeyNotFoundException("No se encontró el caso de revisión.");
        var autorizacion = await _autorizacion.AutorizarResolucionAsync(caso.PeriodoId, cancellationToken);
        var estadoAnterior = caso.Estado;
        var ordenAnterior = caso.ObtenerOrdenDecidido();
        caso.Reabrir(peticion.Motivo, peticion.Revision);
        await GuardarConHistorialAsync(
            caso,
            new HistorialCasoRevision(
                caso,
                "Reabrir",
                autorizacion.UsuarioId,
                peticion.Motivo,
                ordenAnterior,
                caso.ObtenerOrdenDecidido(),
                estadoAnterior,
                caso.Estado),
            cancellationToken);
        return Mapear(caso);
    }

    private async Task GenerarDesdeResultadosAsync(Guid periodoId, CancellationToken cancellationToken)
    {
        var autorizacion = await _autorizacion.AutorizarResolucionAsync(periodoId, cancellationToken);
        var prioridades = await _prioridades.EvaluarAsync(periodoId, cancellationToken);
        var reglas = await _reglas.ConsultarAsync(periodoId, cancellationToken);
        var versionReglas = prioridades.FirstOrDefault()?.VersionReglasId;
        foreach (var sala in prioridades.Where(x => x.Sala.HasValue).Select(x => x.Sala!.Value).Distinct())
        {
            var orden = await _sorteos.CalcularOrdenAsync(periodoId, sala, cancellationToken);
            var pendientes = orden.Where(x => x.RequiereRevision).ToArray();
            foreach (var grupo in pendientes.GroupBy(ObtenerTipoPorMotivo))
            {
                await CrearSiNoExisteAsync(
                    periodoId,
                    sala,
                    grupo.Key,
                    grupo.Select(x => x.SolicitudId).ToArray(),
                    $"sorteo:{orden[0].SorteoId:N}:v{orden[0].VersionSorteo}:tipo:{grupo.Key}:solicitudes:{string.Join(',', grupo.Select(x => x.SolicitudId).OrderBy(x => x))}",
                    orden[0].SorteoId,
                    orden[0].VersionSorteo,
                    versionReglas,
                    cancellationToken);
            }

            await GenerarEmpatesDistanciaAsync(periodoId, autorizacion.InstitucionId, sala, orden, versionReglas, reglas.PrecisionDistanciaDecimales, cancellationToken);
            await GenerarDomiciliosPendientesAsync(periodoId, autorizacion.InstitucionId, sala, orden, versionReglas, cancellationToken);
        }
    }

    private async Task GenerarEmpatesDistanciaAsync(
        Guid periodoId,
        Guid institucionId,
        int sala,
        IReadOnlyList<DatosOrdenSorteo> orden,
        Guid? versionReglas,
        int precisionDecimales,
        CancellationToken cancellationToken)
    {
        var mediciones = new List<(DatosOrdenSorteo Orden, ConsultarDistanciaRespuesta Distancia)>();
        foreach (var participante in orden)
        {
            var vigente = (await _distancias.ConsultarHistorialAsync(
                institucionId,
                participante.SolicitudId,
                cancellationToken)).FirstOrDefault(x => x.EsVigente);
            if (vigente is not null)
                mediciones.Add((participante, vigente));
        }

        foreach (var grupo in mediciones.GroupBy(x => Math.Round(x.Distancia.DistanciaMetros, precisionDecimales, MidpointRounding.AwayFromZero))
                     .Where(x => x.Count() > 1))
        {
            var elementos = grupo.ToArray();
            var huella = $"sorteo:{orden[0].SorteoId:N}:v{orden[0].VersionSorteo}:precision:{precisionDecimales}:distancia:{grupo.Key.ToString(CultureInfo.InvariantCulture)}:revisiones:{string.Join(',', elementos.Select(x => $"{x.Orden.SolicitudId:N}-{x.Distancia.RevisionDomicilio}-{x.Distancia.RevisionUbicacionJardin}").OrderBy(x => x))}";
            await CrearSiNoExisteAsync(
                periodoId,
                sala,
                TipoCasoRevision.EmpateDistancia,
                elementos.Select(x => x.Orden.SolicitudId).ToArray(),
                huella,
                orden[0].SorteoId,
                orden[0].VersionSorteo,
                versionReglas,
                cancellationToken);
        }
    }

    private async Task GenerarDomiciliosPendientesAsync(
        Guid periodoId,
        Guid institucionId,
        int sala,
        IReadOnlyList<DatosOrdenSorteo> orden,
        Guid? versionReglas,
        CancellationToken cancellationToken)
    {
        foreach (var participante in orden)
        {
            var revision = await _revisionesDomicilio.ConsultarAsync(
                institucionId,
                participante.SolicitudId,
                cancellationToken);
            if (string.Equals(revision.Estado, "RevisadoManualmente", StringComparison.OrdinalIgnoreCase))
                continue;

            await CrearSiNoExisteAsync(
                periodoId,
                sala,
                TipoCasoRevision.DomicilioPendiente,
                [participante.SolicitudId],
                $"sorteo:{participante.SorteoId:N}:v{participante.VersionSorteo}:domicilio:{participante.SolicitudId:N}:revision:{revision.RevisionDomicilio}:{revision.Estado}",
                participante.SorteoId,
                participante.VersionSorteo,
                versionReglas,
                cancellationToken);
        }
    }

    private async Task CrearSiNoExisteAsync(
        Guid periodoId,
        int sala,
        TipoCasoRevision tipo,
        IReadOnlyCollection<Guid> solicitudes,
        string huella,
        Guid sorteoId,
        int versionSorteo,
        Guid? versionReglas,
        CancellationToken cancellationToken)
    {
        var existente = await _repositorio.ObtenerPorHuellaAsync(periodoId, sala, tipo, huella, cancellationToken);
        if (existente is not null) return;

        var caso = new CasoRevision(periodoId, sala, tipo, solicitudes, huella, sorteoId, versionSorteo, versionReglas);
        await _repositorio.GuardarAsync(caso, cancellationToken);
    }

    private static TipoCasoRevision ObtenerTipoPorMotivo(DatosOrdenSorteo resultado)
        => resultado.MotivoRevision?.Contains("DNI", StringComparison.OrdinalIgnoreCase) == true
            ? TipoCasoRevision.SinDni
            : TipoCasoRevision.EmpateTerminacionDni;

    private async Task GuardarConHistorialAsync(
        CasoRevision caso,
        HistorialCasoRevision historial,
        CancellationToken cancellationToken)
    {
        await _repositorio.GuardarConHistorialAsync(caso, historial, cancellationToken);
    }

    private static DatosCasoRevision Mapear(CasoRevision caso) => new(
        caso.Id,
        caso.PeriodoId,
        caso.Sala,
        caso.Tipo.ToString(),
        caso.Estado.ToString(),
        caso.ObtenerSolicitudesAfectadas(),
        caso.ObtenerOrdenDecidido(),
        caso.SorteoId,
        caso.VersionSorteo,
        caso.VersionReglasId,
        caso.UsuarioResolucion,
        caso.FechaResolucion,
        caso.MotivoResolucion,
        caso.FechaCreacion,
        caso.Revision);
}
