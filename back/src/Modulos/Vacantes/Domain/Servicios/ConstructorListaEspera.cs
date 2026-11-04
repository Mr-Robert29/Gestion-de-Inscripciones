using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Domain.Servicios;

public sealed record RegistroListaEspera(
    SolicitudPropuesta Solicitud,
    int? PosicionGeneral,
    int? PosicionEspera,
    string Estado,
    string Motivo);

// Deriva la lista del resultado confirmado, sin volver a decidir ingresos ni desempates.
public static class ConstructorListaEspera
{
    public static IReadOnlyList<RegistroListaEspera> Construir(
        IReadOnlyList<SolicitudPropuesta> solicitudesBase,
        IReadOnlyList<ResultadoPropuestaIngreso> resultadosConfirmados,
        IReadOnlyDictionary<Guid, int> posicionesOriginales,
        IReadOnlySet<Guid> aspirantesConIngreso,
        IReadOnlySet<Guid> aspirantesConReserva,
        bool ordenRequiereRevision,
        IReadOnlyList<SolicitudPropuesta> solicitudesNuevas)
    {
        if (solicitudesBase.Select(x => x.Id).Distinct().Count() != solicitudesBase.Count ||
            resultadosConfirmados.Select(x => x.SolicitudId).Distinct().Count() != resultadosConfirmados.Count)
            throw new InvalidOperationException("El resultado contiene solicitudes repetidas.");
        var resultados = resultadosConfirmados.ToDictionary(x => x.SolicitudId);
        if (resultados.Keys.Except(solicitudesBase.Select(x => x.Id)).Any())
            throw new InvalidOperationException("El resultado contiene solicitudes ajenas a su instantánea.");
        if (solicitudesBase.Any(x => resultados.TryGetValue(x.Id, out var resultado) &&
            (resultado.AspiranteId != x.AspiranteId || resultado.Sala != x.Sala)))
            throw new InvalidOperationException("El resultado no coincide con el aspirante y grupo etario registrados.");
        var duplicados = solicitudesBase.GroupBy(x => x.AspiranteId).Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet();
        var posicionesRepetidas = posicionesOriginales.Values.GroupBy(x => x).Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet();
        var registros = new List<RegistroListaEspera>();
        foreach (var solicitud in solicitudesBase)
        {
            if (aspirantesConIngreso.Contains(solicitud.AspiranteId)) continue;
            resultados.TryGetValue(solicitud.Id, out var resultado);
            var posicion = posicionesOriginales.GetValueOrDefault(solicitud.Id);
            var motivo = resultado?.Explicacion ?? "La solicitud no tiene un resultado registrado.";
            var esEspera = resultado?.Estado == "EsperaPropuesta";
            if (solicitud.FueraDeTermino || solicitud.Sala is null || solicitud.DuplicacionPendiente || duplicados.Contains(solicitud.AspiranteId))
            {
                esEspera = false;
                motivo = "La edad, recepción o continuidad de una posible duplicación requiere resolución.";
            }
            else if (esEspera && (posicion <= 0 || posicionesRepetidas.Contains(posicion)))
            {
                esEspera = false;
                motivo = "No se conserva una posición general inequívoca para este resultado.";
            }
            else if (esEspera && ordenRequiereRevision)
            {
                esEspera = false;
                motivo = "Cambiaron datos que pueden afectar el orden confirmado; se requiere revisión sin reordenar automáticamente.";
            }
            if (!esEspera && resultado?.Estado == "IngresoPropuesto")
                motivo = "El resultado indica ingreso propuesto, pero no se encuentra su ingreso confirmado.";
            var estado = esEspera
                ? aspirantesConReserva.Contains(solicitud.AspiranteId) ? "ConReserva" : "EnEspera"
                : "PendienteRevision";
            registros.Add(new RegistroListaEspera(solicitud, esEspera ? posicion : null, null, estado, motivo));
        }
        foreach (var solicitud in solicitudesNuevas)
        {
            if (aspirantesConIngreso.Contains(solicitud.AspiranteId) || solicitudesBase.Any(x => x.Id == solicitud.Id)) continue;
            registros.Add(new RegistroListaEspera(
                solicitud,
                null,
                null,
                "PendienteRevision",
                "Solicitud posterior al resultado confirmado; todavía no tiene una decisión de admisión."));
        }
        var esperas = registros.Where(x => x.Estado != "PendienteRevision").OrderBy(x => x.PosicionGeneral)
            .Select((x, indice) => x with { PosicionEspera = indice + 1 });
        // El Id solo estabiliza la presentación de pendientes; nunca les otorga una posición de admisión.
        return esperas.Concat(registros.Where(x => x.Estado == "PendienteRevision").OrderBy(x => x.Solicitud.Id)).ToArray();
    }
}
