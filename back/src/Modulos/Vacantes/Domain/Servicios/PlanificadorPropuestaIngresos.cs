using System.Globalization;
using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Domain.Servicios;

// Coordina orden, decisiones registradas y factibilidad; no confirma ni reserva plazas.
public static class PlanificadorPropuestaIngresos
{
    public static IReadOnlyList<ResultadoPropuestaIngreso> Generar(
        EntradasPropuestaIngresos entradas,
        IReadOnlyList<Sorteo> sorteos,
        IReadOnlyList<CasoRevision> casos,
        IReadOnlyList<OcupacionPlaza> ocupaciones,
        CancellationToken tokenCancelacion = default)
    {
        var ofertas = entradas.Ofertas.Where(x => x.Disponible).ToArray();
        if (ocupaciones.Any(x => x.PeriodoId != entradas.PeriodoId) ||
            ocupaciones.Select(x => x.AspiranteId).Distinct().Count() != ocupaciones.Count)
            throw new InvalidOperationException("La ocupación contiene plazas duplicadas o ajenas al período.");
        if (ocupaciones.Any(x => ofertas.All(o => o.Id != x.OfertaId)))
            throw new InvalidOperationException("Una ocupación activa no pertenece a la oferta disponible publicada.");
        var recursos = ofertas.Select(o => new RecursoProvisional(
            o.Id,
            o.TurnoId,
            o.Capacidad,
            o.EdadesOcupacionesIniciales.Concat(ocupaciones.Where(x => x.OfertaId == o.Id).Select(x => x.Sala)).Distinct().ToArray(),
            checked((o.CantidadOcupacionInicial ?? o.EdadesOcupacionesIniciales.Count) + ocupaciones.Count(x => x.OfertaId == o.Id)))).ToArray();
        if (entradas.Solicitudes.Select(x => x.Id).Distinct().Count() != entradas.Solicitudes.Count)
            throw new InvalidOperationException("Una solicitud aparece más de una vez.");
        // También valida que la ocupación existente permita una composición aprobada.
        ComprobadorDistribucionProvisional.Buscar(recursos, [], entradas.Combinaciones, tokenCancelacion);
        var salida = new List<ResultadoPropuestaIngreso>();
        var elegidos = new List<CandidatoProvisional>();
        IReadOnlyList<AsignacionProvisional> distribucion = [];
        var turnosPendientes = new HashSet<Guid>();
        var solicitudes = entradas.Solicitudes.ToDictionary(x => x.Id);
        var repetidos = entradas.Solicitudes.GroupBy(x => x.AspiranteId).Where(x => x.Count() > 1)
            .SelectMany(x => x.Select(s => s.Id)).ToHashSet();
        var ordinarias = new List<SolicitudPropuesta>();
        foreach (var solicitud in entradas.Solicitudes)
        {
            var ocupacion = ocupaciones.FirstOrDefault(x => x.AspiranteId == solicitud.AspiranteId);
            if (ocupacion is not null)
            {
                salida.Add(Resultado(solicitud, "YaOcupaPlaza", "El aspirante ya tiene un ingreso o reserva activa; no se propone otra plaza."));
                continue;
            }
            if (solicitud.FueraDeTermino)
            {
                salida.Add(Resultado(solicitud, "PendienteRevision", "Solicitud fuera de término, pendiente de autorización."));
                continue;
            }
            if (solicitud.Sala is null || !entradas.OrdenSalas.Contains(solicitud.Sala.Value))
            {
                salida.Add(Resultado(solicitud, "PendienteRevision", "No se puede ubicar la edad en el orden del período."));
                turnosPendientes.UnionWith(ofertas.Select(x => x.TurnoId));
                continue;
            }
            ordinarias.Add(solicitud);
        }
        var evaluacion = EvaluadorPrioridades.Evaluar(
            ordinarias.Select(x => new EntradaEvaluacionPrioridad(
                x.Id,
                x.AspiranteId,
                x.Sala,
                false,
                x.Condiciones.ToDictionary(c => c, _ => true, StringComparer.Ordinal),
                x.DistanciaMetros)),
            entradas.OrdenSalas,
            entradas.OrdenProcedimiento,
            entradas.PrecisionDistanciaDecimales);
        foreach (var sala in entradas.OrdenSalas)
        {
            var grupoSala = evaluacion.Where(x => x.Sala == sala).Select(x => solicitudes[x.SolicitudId]).ToArray();
            var faltaDistancia = entradas.OrdenProcedimiento.Contains("Cercania") && grupoSala.Any(x => x.DistanciaMetros is null);
            foreach (var grupo in grupoSala.GroupBy(x => ClavePrioridad(
                x,
                entradas.OrdenProcedimiento,
                entradas.PrecisionDistanciaDecimales)))
            {
                var iguales = grupo.ToArray();
                string? motivo = faltaDistancia ? "Falta distancia vigente en la sala; no se puede asegurar el orden por cercanía." : null;
                if (iguales.Any(x => x.DuplicacionPendiente || repetidos.Contains(x.Id)))
                    motivo = "Hay una posible duplicación sin procedimiento de continuidad resuelto.";
                if (motivo is null && casos.Any(c => c.Tipo == TipoCasoRevision.DomicilioPendiente &&
                    c.Estado != EstadoCasoRevision.Resuelto && c.ObtenerSolicitudesAfectadas().Intersect(iguales.Select(x => x.Id)).Any()))
                    motivo = "Hay una revisión de domicilio pendiente.";
                var sorteoVigente = sorteos.SingleOrDefault(x => x.Sala == sala);
                if (motivo is null && casos.Any(c => c.Tipo != TipoCasoRevision.DomicilioPendiente &&
                    c.Estado != EstadoCasoRevision.Resuelto && c.VersionReglasId == entradas.VersionReglasId &&
                    c.SorteoId == sorteoVigente?.Id && c.VersionSorteo == sorteoVigente?.Version &&
                    c.ObtenerSolicitudesAfectadas().Count > 0 &&
                    c.ObtenerSolicitudesAfectadas().All(id => iguales.Any(x => x.Id == id))))
                    motivo = "Hay un caso de revisión abierto para este grupo y las versiones utilizadas.";
                var orden = motivo is null ? OrdenarGrupo(iguales, entradas, sorteos, casos) : null;
                if (orden is null) motivo ??= "El sorteo o la decisión manual vigente no permiten determinar el orden de este grupo.";
                if (motivo is not null)
                {
                    foreach (var solicitud in iguales)
                    {
                        salida.Add(Resultado(solicitud, "PendienteRevision", motivo));
                        turnosPendientes.UnionWith(Turnos(solicitud));
                    }
                    continue;
                }
                foreach (var solicitud in orden!)
                {
                    if (Turnos(solicitud).Any(turnosPendientes.Contains))
                    {
                        salida.Add(Resultado(solicitud, "PendienteRevision", "Un grupo anterior pendiente compite por los turnos aceptados; no se lo adelanta automáticamente."));
                        turnosPendientes.UnionWith(Turnos(solicitud));
                        continue;
                    }
                    var candidato = new CandidatoProvisional(solicitud.Id, sala, solicitud.TurnoPreferidoId, solicitud.Alternativas);
                    var intento = ComprobadorDistribucionProvisional.Buscar(
                        recursos,
                        elegidos.Append(candidato).ToArray(),
                        entradas.Combinaciones,
                        tokenCancelacion);
                    if (intento is null)
                        salida.Add(Resultado(solicitud, "EsperaPropuesta", "No hay una distribución factible sin desplazar candidatos anteriores y respetando los turnos aceptados."));
                    else
                    {
                        elegidos.Add(candidato);
                        distribucion = intento;
                        salida.Add(Resultado(solicitud, "IngresoPropuesto", "Lugar propuesto con distribución provisional factible; requiere revisión y confirmación posterior."));
                    }
                }
            }
        }
        var asignaciones = distribucion.ToDictionary(x => x.SolicitudId);
        return salida.Select(x => asignaciones.TryGetValue(x.SolicitudId, out var a)
            ? x with { OfertaId = a.OfertaId, TurnoId = a.TurnoId, EsTurnoAlternativo = a.TurnoId != solicitudes[x.SolicitudId].TurnoPreferidoId }
            : x).ToArray();
    }

    private static SolicitudPropuesta[]? OrdenarGrupo(
        SolicitudPropuesta[] grupo,
        EntradasPropuestaIngresos entradas,
        IReadOnlyList<Sorteo> sorteos,
        IReadOnlyList<CasoRevision> casos)
    {
        if (grupo.Length == 1) return grupo;
        var sorteo = sorteos.SingleOrDefault(x => x.Sala == grupo[0].Sala);
        var manual = BuscarResolucion(grupo, entradas.VersionReglasId, sorteo, casos);
        if (manual is not null) return manual;
        if (sorteo is null || !entradas.OrdenProcedimiento.Contains("SorteoUltimaInstancia")) return null;
        var orden = OrdenadorSorteoCircular.Ordenar(
            sorteo.Numero,
            grupo.Select(x => new ParticipanteSorteo(x.Id, x.AspiranteId, x.Sala!.Value, x.Dni)));
        if (orden.Any(x => x.ClaveCircular is null)) return null;
        var resultado = new List<SolicitudPropuesta>();
        foreach (var bloque in orden.OrderBy(x => x.ClaveCircular).GroupBy(x => x.ClaveCircular))
        {
            var afectados = bloque.Select(x => grupo.Single(s => s.Id == x.SolicitudId)).ToArray();
            if (afectados.Length == 1) resultado.Add(afectados[0]);
            else
            {
                var resueltos = BuscarResolucion(afectados, entradas.VersionReglasId, sorteo, casos);
                if (resueltos is null) return null;
                resultado.AddRange(resueltos);
            }
        }
        return resultado.ToArray();
    }

    private static SolicitudPropuesta[]? BuscarResolucion(
        SolicitudPropuesta[] grupo,
        Guid versionReglasId,
        Sorteo? sorteo,
        IReadOnlyList<CasoRevision> casos)
    {
        var ids = grupo.Select(x => x.Id).ToHashSet();
        var validos = casos.Where(c => c.Tipo != TipoCasoRevision.DomicilioPendiente &&
            c.Estado == EstadoCasoRevision.Resuelto && c.VersionReglasId == versionReglasId &&
            c.SorteoId == sorteo?.Id && c.VersionSorteo == sorteo?.Version &&
            c.FechaResolucion >= grupo.Max(x => x.UltimoCambio) &&
            ids.SetEquals(c.ObtenerSolicitudesAfectadas()) && ids.SetEquals(c.ObtenerOrdenDecidido())).ToArray();
        if (validos.Length == 0) return null;
        var orden = validos[0].ObtenerOrdenDecidido();
        if (validos.Any(x => !x.ObtenerOrdenDecidido().SequenceEqual(orden))) return null;
        return orden.Select(id => grupo.Single(x => x.Id == id)).ToArray();
    }

    private static string ClavePrioridad(
        SolicitudPropuesta solicitud,
        IReadOnlyList<string> criterios,
        int precisionDecimales)
        => string.Join('|', criterios.Where(c => c is not ("SorteoUltimaInstancia" or "FueraDeTermino"))
            .Select(c => c == "Cercania" ? (solicitud.DistanciaMetros is decimal distancia
                    ? Math.Round(distancia, precisionDecimales, MidpointRounding.AwayFromZero).ToString("G29", CultureInfo.InvariantCulture)
                    : "sin-distancia")
                : solicitud.Condiciones.Contains(c, StringComparer.Ordinal) ? "1" : "0"));

    private static IEnumerable<Guid> Turnos(SolicitudPropuesta solicitud)
        => solicitud.Alternativas.Prepend(solicitud.TurnoPreferidoId).Distinct();

    private static ResultadoPropuestaIngreso Resultado(
        SolicitudPropuesta solicitud,
        string estado,
        string explicacion)
        => new(solicitud.Id, solicitud.AspiranteId, solicitud.Sala, estado, null, null, false, explicacion);
}

