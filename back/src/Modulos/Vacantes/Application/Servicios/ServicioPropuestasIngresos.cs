using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jardin.Vacantes.Application.Persistencia;
using Jardin.Vacantes.Application.Propuestas;
using Jardin.Vacantes.Application.Seguridad;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Domain.Servicios;

namespace Jardin.Vacantes.Application.Servicios;

public sealed class ServicioPropuestasIngresos : IModuloPropuestasIngresos
{
    private readonly IAutorizacionPropuestasIngresos _autorizacion;
    private readonly IContextoPropuestaIngresos _contexto;
    private readonly IRepositorioPropuestasIngresos _propuestas;
    private readonly IRepositorioSorteos _sorteos;
    private readonly IRepositorioCasosRevision _casos;

    public ServicioPropuestasIngresos(
        IAutorizacionPropuestasIngresos autorizacion,
        IContextoPropuestaIngresos contexto,
        IRepositorioPropuestasIngresos propuestas,
        IRepositorioSorteos sorteos,
        IRepositorioCasosRevision casos)
    {
        _autorizacion = autorizacion;
        _contexto = contexto;
        _propuestas = propuestas;
        _sorteos = sorteos;
        _casos = casos;
    }

    // Implementa IModuloPropuestasIngresos.GenerarAsync: coordina una nueva propuesta, sin confirmar ingresos.
    public async Task<ConsultarPropuestaIngresosRespuesta> GenerarAsync(
        Guid periodoId,
        GenerarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        var autor = await _autorizacion.AutorizarPropuestaAsync(periodoId, tokenCancelacion);
        return await _contexto.ConPeriodoProtegidoAsync(
            periodoId,
            async token =>
            {
                var datos = await LeerAsync(periodoId, token);
                if (datos.Entradas.EstadoPeriodo != "RecepcionCerrada")
                    throw new InvalidOperationException("Cerrá la recepción antes de generar una propuesta.");
                if (datos.Entradas.InstitucionId != autor.InstitucionId)
                    throw new UnauthorizedAccessException();
                var resultados = PlanificadorPropuestaIngresos.Generar(
                    datos.Entradas,
                    datos.Sorteos,
                    datos.Casos,
                    datos.Ocupaciones,
                    token);
                var huella = Huella(datos);
                if (Huella(await LeerAsync(periodoId, token)) != huella)
                    throw new InvalidOperationException("Las entradas cambiaron durante el cálculo. Generá otra propuesta.");
                var propuesta = new PropuestaIngresos(
                    periodoId,
                    autor.InstitucionId,
                    autor.UsuarioId,
                    peticion.Motivo,
                    huella,
                    JsonSerializer.Serialize(datos),
                    JsonSerializer.Serialize(resultados));
                await _propuestas.GuardarAsync(propuesta, token);
                return Mapear(propuesta, true);
            },
            tokenCancelacion);
    }

    // Implementa IModuloPropuestasIngresos.ConsultarAsync: conserva el resultado aun si sus entradas cambiaron.
    public async Task<ConsultarPropuestaIngresosRespuesta?> ConsultarAsync(Guid propuestaId, CancellationToken tokenCancelacion = default)
    {
        var propuesta = await _propuestas.ObtenerAsync(propuestaId, tokenCancelacion);
        if (propuesta is null) return null;
        await _autorizacion.AutorizarPropuestaAsync(propuesta.PeriodoId, tokenCancelacion);
        return Mapear(propuesta, await EstaVigenteAsync(propuesta, tokenCancelacion));
    }

    // Implementa IModuloPropuestasIngresos.ListarAsync: devuelve versiones históricas con su vigencia observada.
    public async Task<IReadOnlyList<ConsultarPropuestaIngresosRespuesta>> ListarAsync(Guid periodoId, CancellationToken tokenCancelacion = default)
    {
        await _autorizacion.AutorizarPropuestaAsync(periodoId, tokenCancelacion);
        var propuestas = await _propuestas.ListarAsync(periodoId, tokenCancelacion);
        var respuesta = new List<ConsultarPropuestaIngresosRespuesta>();
        foreach (var propuesta in propuestas)
            respuesta.Add(Mapear(propuesta, await EstaVigenteAsync(propuesta, tokenCancelacion)));
        return respuesta;
    }

    public async Task<ConsultarPropuestaIngresosRespuesta> ModificarAsync(
        Guid propuestaId,
        ModificarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.HuellaEsperada);
        ArgumentException.ThrowIfNullOrWhiteSpace(peticion.Motivo);
        if (peticion.Cambios.Count == 0 || peticion.Cambios.Select(x => x.SolicitudId).Distinct().Count() != peticion.Cambios.Count)
            throw new ArgumentException("Indicá cambios distintos para la propuesta.");

        var propuesta = await _propuestas.ObtenerAsync(propuestaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la propuesta.");
        var autor = await _autorizacion.AutorizarModificacionAsync(propuesta.PeriodoId, tokenCancelacion);
        if (!string.Equals(propuesta.HuellaEntradas, peticion.HuellaEsperada, StringComparison.Ordinal) ||
            !await EstaVigenteAsync(propuesta, tokenCancelacion))
            throw new InvalidOperationException("La propuesta está desactualizada. Generá una nueva propuesta.");

        var resultados = DeserializarResultados(propuesta).ToDictionary(x => x.SolicitudId);
        foreach (var cambio in peticion.Cambios)
        {
            if (!resultados.TryGetValue(cambio.SolicitudId, out var anterior))
                throw new ArgumentException("La solicitud no pertenece a la propuesta.");
            ValidarResultadoManual(cambio, anterior);
            resultados[cambio.SolicitudId] = new DatosResultadoPropuesta(
                cambio.SolicitudId,
                anterior.AspiranteId,
                anterior.Sala,
                cambio.Estado,
                cambio.OfertaId,
                cambio.TurnoId,
                cambio.EsTurnoAlternativo,
                cambio.Explicacion);
        }

        var datosActuales = await LeerAsync(propuesta.PeriodoId, tokenCancelacion);
        if (Huella(datosActuales) != propuesta.HuellaEntradas)
            throw new InvalidOperationException("La propuesta está desactualizada. Generá una nueva propuesta.");
        var resultadosModificados = resultados.Values.Where(x => x.Estado == "IngresoPropuesto").ToArray();
        ValidarConfirmacion(datosActuales, resultadosModificados);
        ValidarCasosPendientes(datosActuales, resultadosModificados);
        await _propuestas.ActualizarEstadoAsync(propuesta.Id, EstadoPropuestaIngreso.Desactualizada, propuesta.Revision, tokenCancelacion);

        var nueva = new PropuestaIngresos(
            propuesta.PeriodoId,
            autor.InstitucionId,
            autor.UsuarioId,
            peticion.Motivo,
            propuesta.HuellaEntradas,
            propuesta.Entradas,
            JsonSerializer.Serialize(resultados.Values.OrderBy(x => x.SolicitudId)),
            propuesta.Id);
        await _propuestas.GuardarAsync(nueva, tokenCancelacion);
        return Mapear(nueva, true);
    }

    public async Task<DatosAceptacionTurno> RegistrarAceptacionTurnoAsync(
        Guid propuestaId,
        RegistrarAceptacionTurnoPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var propuesta = await _propuestas.ObtenerAsync(propuestaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la propuesta.");
        var autor = await _autorizacion.AutorizarAceptacionTurnoAsync(propuesta.PeriodoId, tokenCancelacion);
        var resultado = DeserializarResultados(propuesta).SingleOrDefault(x => x.SolicitudId == peticion.SolicitudId)
            ?? throw new ArgumentException("La solicitud no pertenece a la propuesta.");
        if (!resultado.EsTurnoAlternativo || resultado.TurnoId != peticion.TurnoId)
            throw new ArgumentException("La solicitud no tiene ese turno alternativo pendiente de aceptación.");
        if (!await EstaVigenteAsync(propuesta, tokenCancelacion))
            throw new InvalidOperationException("La propuesta está desactualizada.");

        var existente = await _propuestas.ObtenerAceptacionAsync(propuesta.Id, peticion.SolicitudId, peticion.TurnoId, tokenCancelacion);
        if (existente is not null) return Mapear(existente);
        var aceptacion = new AceptacionTurnoFamiliar(
            propuesta.PeriodoId,
            propuesta.Id,
            peticion.SolicitudId,
            peticion.TurnoId,
            autor.UsuarioId,
            peticion.Referencia);
        await _propuestas.GuardarAceptacionAsync(aceptacion, tokenCancelacion);
        return Mapear(aceptacion);
    }

    public async Task<IReadOnlyList<DatosConfirmacionIngreso>> ConfirmarAsync(
        Guid propuestaId,
        ConfirmarPropuestaIngresosPeticion peticion,
        CancellationToken tokenCancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);
        var propuesta = await _propuestas.ObtenerAsync(propuestaId, tokenCancelacion)
            ?? throw new KeyNotFoundException("No se encontró la propuesta.");
        var autor = await _autorizacion.AutorizarConfirmacionAsync(propuesta.PeriodoId, tokenCancelacion);
        var resultadosPropuesta = DeserializarResultados(propuesta)
            .Where(x => x.Estado == "IngresoPropuesto")
            .ToArray();
        if (propuesta.Estado == EstadoPropuestaIngreso.Confirmada)
        {
            var ingresosExistentes = await ObtenerIngresosConfirmadosAsync(propuesta.PeriodoId, resultadosPropuesta, tokenCancelacion);
            if (ingresosExistentes.Count == resultadosPropuesta.Length)
                return ingresosExistentes.Select(Mapear).ToArray();

            throw new InvalidOperationException("La propuesta está confirmada, pero sus ingresos no están completos.");
        }
        if (!string.Equals(propuesta.HuellaEntradas, peticion.HuellaEsperada, StringComparison.Ordinal) ||
            !await EstaVigenteAsync(propuesta, tokenCancelacion))
            throw new InvalidOperationException("La propuesta está desactualizada y no puede confirmarse.");

        return await _contexto.ConPeriodoProtegidoAsync(
            propuesta.PeriodoId,
            async token =>
            {
                var datos = await LeerAsync(propuesta.PeriodoId, token);
                var resultados = resultadosPropuesta;
                if (resultados.Length == 0)
                    throw new InvalidOperationException("La propuesta no tiene ingresos confirmables.");
                ValidarConfirmacion(datos, resultados);
                ValidarCasosPendientes(datos, resultados);
                var reevaluados = PlanificadorPropuestaIngresos.Generar(
                    datos.Entradas,
                    datos.Sorteos,
                    datos.Casos,
                    datos.Ocupaciones,
                    token);
                var reevaluadosPorSolicitud = reevaluados.ToDictionary(x => x.SolicitudId);
                if (resultados.Any(x => !reevaluadosPorSolicitud.TryGetValue(x.SolicitudId, out var reevaluado) || reevaluado.Estado != "IngresoPropuesto"))
                    throw new InvalidOperationException("La factibilidad cambió; la propuesta debe revisarse antes de confirmar.");
                var ingresos = new List<IngresoConfirmado>();
                foreach (var resultado in resultados)
                {
                    if (resultado.EsTurnoAlternativo)
                    {
                        if (resultado.TurnoId is null || await _propuestas.ObtenerAceptacionAsync(propuesta.Id, resultado.SolicitudId, resultado.TurnoId.Value, token) is null)
                            throw new InvalidOperationException("Falta la aceptación familiar del turno alternativo.");
                    }
                    var solicitud = datos.Entradas.Solicitudes.Single(x => x.Id == resultado.SolicitudId);
                    var oferta = datos.Entradas.Ofertas.Single(x => x.Id == resultado.OfertaId);
                    ingresos.Add(new IngresoConfirmado(
                        propuesta.PeriodoId,
                        solicitud.Id,
                        solicitud.AspiranteId,
                        solicitud.Edad,
                        oferta.Id,
                        resultado.TurnoId!.Value,
                        solicitud.Sala!.Value,
                        propuesta.Id,
                        autor.UsuarioId));
                }
                var confirmados = await _propuestas.GuardarConfirmacionesAsync(ingresos, token);
                try
                {
                    await _propuestas.ActualizarEstadoAsync(propuesta.Id, EstadoPropuestaIngreso.Confirmada, propuesta.Revision, token);
                }
                catch (InvalidOperationException)
                {
                    var propuestaActualizada = await _propuestas.ObtenerAsync(propuesta.Id, token);
                    var ingresosExistentes = propuestaActualizada?.Estado == EstadoPropuestaIngreso.Confirmada
                        ? await ObtenerIngresosConfirmadosAsync(propuesta.PeriodoId, resultados, token)
                        : [];
                    if (propuestaActualizada?.Estado != EstadoPropuestaIngreso.Confirmada || ingresosExistentes.Count != resultados.Length)
                        throw;
                    confirmados = ingresosExistentes;
                }
                return confirmados.Select(Mapear).ToArray();
            },
            tokenCancelacion);
    }

    private async Task<IReadOnlyList<IngresoConfirmado>> ObtenerIngresosConfirmadosAsync(
        Guid periodoId,
        IReadOnlyList<DatosResultadoPropuesta> resultados,
        CancellationToken tokenCancelacion)
    {
        var aspirantes = resultados.Select(x => x.AspiranteId).ToHashSet();
        if (aspirantes.Count == 0) return [];
        var ingresos = await _propuestas.ListarIngresosAsync(periodoId, tokenCancelacion);
        return ingresos.Where(x => aspirantes.Contains(x.AspiranteId)).ToArray();
    }

    private async Task<bool> EstaVigenteAsync(PropuestaIngresos propuesta, CancellationToken tokenCancelacion)
    {
        try { return Huella(await LeerAsync(propuesta.PeriodoId, tokenCancelacion)) == propuesta.HuellaEntradas; }
        catch (InvalidOperationException) { return false; }
        catch (KeyNotFoundException) { return false; }
    }

    private async Task<Instantanea> LeerAsync(Guid periodoId, CancellationToken tokenCancelacion)
    {
        var entradas = await _contexto.LeerAsync(periodoId, tokenCancelacion);
        var bajas = (await _propuestas.ListarIngresosAsync(periodoId, tokenCancelacion))
            .Where(x => !x.Activo || x.BajaEnProceso).Select(x => x.AspiranteId).ToHashSet();
        entradas = entradas with { Solicitudes = entradas.Solicitudes.Where(x => !bajas.Contains(x.AspiranteId)).ToArray() };
        return new Instantanea(
            entradas,
            (await _sorteos.ListarVigentesAsync(periodoId, tokenCancelacion)).OrderBy(x => x.Id).ToArray(),
            (await _casos.ListarAsync(periodoId, tokenCancelacion)).OrderBy(x => x.Id).ToArray(),
            (await _propuestas.ListarOcupacionesAsync(periodoId, tokenCancelacion)).OrderBy(x => x.Id).ToArray());
    }

    private static string Huella(Instantanea datos)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(datos))));

    private static ConsultarPropuestaIngresosRespuesta Mapear(PropuestaIngresos propuesta, bool vigente)
    {
        using var instantanea = JsonDocument.Parse(propuesta.Entradas);
        var entradas = instantanea.RootElement.GetProperty("Entradas").Deserialize<EntradasPropuestaIngresos>()!;
        return new ConsultarPropuestaIngresosRespuesta(
            propuesta.Id,
            propuesta.InstitucionId,
            propuesta.PeriodoId,
            propuesta.UsuarioId,
            propuesta.Fecha,
            propuesta.Motivo,
            entradas.VersionReglasId,
            entradas.VersionOfertaId,
            vigente ? propuesta.Estado.ToString() : nameof(EstadoPropuestaIngreso.Desactualizada),
            propuesta.Revision,
            propuesta.PropuestaAnteriorId,
            vigente,
            propuesta.HuellaEntradas,
            JsonSerializer.Deserialize<DatosResultadoPropuesta[]>(propuesta.Resultados)!);
    }

    private static IReadOnlyList<DatosResultadoPropuesta> DeserializarResultados(PropuestaIngresos propuesta)
        => JsonSerializer.Deserialize<DatosResultadoPropuesta[]>(propuesta.Resultados) ?? [];

    private static void ValidarResultadoManual(
        ModificarResultadoPropuestaPeticion cambio,
        DatosResultadoPropuesta anterior)
    {
        if (cambio.Estado is not ("IngresoPropuesto" or "EsperaPropuesta" or "PendienteRevision"))
            throw new ArgumentException("El estado manual no es válido.");
        if (cambio.Estado == "IngresoPropuesto" && (cambio.OfertaId is null || cambio.TurnoId is null))
            throw new ArgumentException("Un ingreso propuesto requiere oferta y turno.");
        if (cambio.Estado != "IngresoPropuesto" && cambio.EsTurnoAlternativo)
            throw new ArgumentException("Solo un ingreso propuesto puede usar turno alternativo.");
        if (anterior.Estado == "YaOcupaPlaza")
            throw new InvalidOperationException("No se puede modificar una solicitud que ya ocupa una plaza.");
    }

    private static void ValidarConfirmacion(Instantanea datos, IReadOnlyList<DatosResultadoPropuesta> resultados)
    {
        var ofertas = datos.Entradas.Ofertas.Where(x => x.Disponible).ToDictionary(x => x.Id);
        var cantidades = datos.Ocupaciones.GroupBy(x => x.OfertaId).ToDictionary(x => x.Key, x => x.Count());
        var edades = datos.Entradas.Ofertas.ToDictionary(
            x => x.Id,
            x => x.EdadesOcupacionesIniciales.Concat(datos.Ocupaciones.Where(o => o.OfertaId == x.Id).Select(o => o.Sala)).ToList());
        foreach (var resultado in resultados)
        {
            if (resultado.OfertaId is null || resultado.TurnoId is null || resultado.Sala is null || !ofertas.TryGetValue(resultado.OfertaId.Value, out var oferta))
                throw new InvalidOperationException("La propuesta contiene una asignación sin oferta vigente.");
            if (oferta.TurnoId != resultado.TurnoId.Value)
                throw new InvalidOperationException("La propuesta contiene un turno incompatible con la oferta.");
            cantidades[oferta.Id] = cantidades.GetValueOrDefault(oferta.Id) + 1;
            if (cantidades[oferta.Id] > oferta.Capacidad)
                throw new InvalidOperationException("La capacidad disponible cambió durante la confirmación.");
            var solicitud = datos.Entradas.Solicitudes.Single(x => x.Id == resultado.SolicitudId);
            edades[oferta.Id].Add(solicitud.Edad);
            if (!datos.Entradas.Combinaciones.Any(combinacion => edades[oferta.Id].Distinct().All(combinacion.Contains)))
                throw new InvalidOperationException("La modificación no es compatible con las combinaciones de edades permitidas.");
        }
        if (resultados.Select(x => x.AspiranteId).Distinct().Count() != resultados.Count)
            throw new InvalidOperationException("Un aspirante aparece más de una vez en la confirmación.");
    }

    private static void ValidarCasosPendientes(Instantanea datos, IReadOnlyList<DatosResultadoPropuesta> resultados)
    {
        var solicitudes = resultados.Select(x => x.SolicitudId).ToHashSet();
        var pendientes = datos.Casos.Where(x => x.Estado != EstadoCasoRevision.Resuelto &&
            x.ObtenerSolicitudesAfectadas().Any(solicitudes.Contains)).ToArray();
        if (pendientes.Length > 0)
            throw new InvalidOperationException("La propuesta tiene casos de revisión pendientes que afectan los ingresos a confirmar.");
    }

    private static DatosAceptacionTurno Mapear(AceptacionTurnoFamiliar aceptacion) => new(
        aceptacion.Id,
        aceptacion.PropuestaId,
        aceptacion.SolicitudId,
        aceptacion.TurnoId,
        aceptacion.UsuarioId,
        aceptacion.Fecha,
        aceptacion.Referencia);

    private static DatosConfirmacionIngreso Mapear(IngresoConfirmado ingreso) => new(
        ingreso.Id,
        ingreso.PeriodoId,
        ingreso.SolicitudId,
        ingreso.AspiranteId,
        ingreso.Edad,
        ingreso.OfertaId,
        ingreso.TurnoId,
        ingreso.Sala,
        ingreso.PropuestaId,
        ingreso.UsuarioId,
        ingreso.Fecha,
        ingreso.Revision);

    private sealed record Instantanea(
        EntradasPropuestaIngresos Entradas,
        IReadOnlyList<Sorteo> Sorteos,
        IReadOnlyList<CasoRevision> Casos,
        IReadOnlyList<OcupacionPlaza> Ocupaciones);
}
