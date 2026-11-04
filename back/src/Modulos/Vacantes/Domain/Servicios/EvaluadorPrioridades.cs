using Jardin.Vacantes.Domain.Entidades;

namespace Jardin.Vacantes.Domain.Servicios;

public static class EvaluadorPrioridades
{
    private static readonly HashSet<string> CriteriosAdmitidos = new(StringComparer.Ordinal)
    {
        "ArticulacionPedagogica",
        "HermanosYGruposConvivientes",
        "Cercania",
        "HijosDelPersonal",
        "SorteoUltimaInstancia",
        "FueraDeTermino"
    };

    public static IReadOnlyList<ResultadoPrioridad> Evaluar(
        IEnumerable<EntradaEvaluacionPrioridad> entradas,
        IReadOnlyList<int> ordenSalas,
        IReadOnlyList<string> ordenProcedimiento,
        int? precisionDistanciaDecimales = null)
    {
        ArgumentNullException.ThrowIfNull(entradas);
        ArgumentNullException.ThrowIfNull(ordenSalas);
        ArgumentNullException.ThrowIfNull(ordenProcedimiento);
        if (precisionDistanciaDecimales is < 0 or > 28)
            throw new ArgumentOutOfRangeException(nameof(precisionDistanciaDecimales));
        if (ordenSalas.Count == 0 || ordenSalas.Distinct().Count() != ordenSalas.Count)
            throw new ArgumentException("El orden de salas debe tener valores únicos.", nameof(ordenSalas));
        if (ordenProcedimiento.Count == 0 || ordenProcedimiento.Distinct(StringComparer.Ordinal).Count() != ordenProcedimiento.Count)
            throw new ArgumentException("El orden de prioridades debe tener valores únicos.", nameof(ordenProcedimiento));
        if (ordenProcedimiento.Any(x => !CriteriosAdmitidos.Contains(x)))
            throw new ArgumentException("La configuración contiene un criterio de prioridad no admitido.", nameof(ordenProcedimiento));

        var lista = entradas.ToArray();
        if (lista.Any(x => x.Sala is < 3 or > 5))
            throw new ArgumentOutOfRangeException(nameof(entradas), "Las solicitudes deben pertenecer a una sala entre 3 y 5.");
        if (lista.Select(x => x.SolicitudId).Distinct().Count() != lista.Length)
            throw new InvalidOperationException("Una solicitud no puede evaluarse más de una vez.");

        var resultados = new List<ResultadoPrioridad>(lista.Length);
        var posicion = 1;
        foreach (var pendiente in lista.Where(x => x.Sala is null))
        {
            resultados.Add(new ResultadoPrioridad(
                pendiente.SolicitudId,
                pendiente.AspiranteId,
                null,
                null,
                true,
                "No se pudo determinar la sala etaria de la solicitud.",
                [],
                ["La edad o la fecha de corte requiere revisión."]));
        }
        foreach (var sala in ordenSalas)
        {
            var grupo = lista.Where(x => x.Sala == sala).ToArray();
            var ordenado = grupo
                .Select(x => EvaluarEntrada(x, ordenProcedimiento, precisionDistanciaDecimales))
                .OrderBy(x => x, ComparadorInterno.Instance)
                .ToArray();

            for (var indice = 0; indice < ordenado.Length; indice++)
            {
                var actual = ordenado[indice];
                var empate = indice > 0 && ComparadorInterno.Instance.Compare(ordenado[indice - 1], actual) == 0;
                var requiereRevision = actual.RequiereRevision || empate ||
                    (indice + 1 < ordenado.Length && ComparadorInterno.Instance.Compare(actual, ordenado[indice + 1]) == 0);
                resultados.Add(new ResultadoPrioridad(
                    actual.Entrada.SolicitudId,
                    actual.Entrada.AspiranteId,
                    actual.Entrada.Sala,
                    requiereRevision ? null : posicion++,
                    requiereRevision,
                    requiereRevision ? actual.MotivoRevision ?? "No se pudo determinar una posición única con los criterios aplicables." : null,
                    actual.CriteriosAplicados,
                    actual.Explicacion));
            }
        }

        return resultados;
    }

    private static EvaluacionInterna EvaluarEntrada(
        EntradaEvaluacionPrioridad entrada,
        IReadOnlyList<string> procedimiento,
        int? precisionDistanciaDecimales)
    {
        var claves = new List<decimal>(procedimiento.Count);
        var aplicados = new List<string>();
        var explicacion = new List<string>();
        string? motivo = null;
        var requiereRevision = false;

        foreach (var criterio in procedimiento)
        {
            if (criterio is "SorteoUltimaInstancia" or "FueraDeTermino")
                continue;

            if (criterio == "Cercania")
            {
                if (entrada.DistanciaMetros is null)
                {
                    requiereRevision = true;
                    motivo ??= "Falta una distancia vigente para aplicar el criterio de cercanía.";
                    claves.Add(decimal.MaxValue);
                    continue;
                }
                var distancia = entrada.DistanciaMetros.Value;
                if (distancia < 0)
                    throw new ArgumentException("La distancia no puede ser negativa.");
                claves.Add(precisionDistanciaDecimales is int precision
                    ? Math.Round(distancia, precision, MidpointRounding.AwayFromZero)
                    : distancia);
                aplicados.Add(criterio);
                explicacion.Add($"Cercanía: {distancia} metros.");
                if (precisionDistanciaDecimales is int decimales)
                    explicacion.Add($"Comparación con {decimales} decimales; se conserva el valor original.");
                continue;
            }

            var cumple = entrada.TieneCondicion(criterio);
            claves.Add(cumple ? 0 : 1);
            aplicados.Add(criterio);
            explicacion.Add(cumple
                ? $"Criterio verificado: {criterio}."
                : $"Criterio no verificado: {criterio}.");
        }

        if (procedimiento.Contains("FueraDeTermino", StringComparer.Ordinal))
        {
            claves.Add(entrada.FueraDeTermino ? 1 : 0);
            explicacion.Add(entrada.FueraDeTermino
                ? "Solicitud fuera de término."
                : "Solicitud recibida dentro de término.");
        }

        return new EvaluacionInterna(entrada, claves, requiereRevision, motivo, aplicados, explicacion);
    }

    private sealed record EvaluacionInterna(
        EntradaEvaluacionPrioridad Entrada,
        IReadOnlyList<decimal> Claves,
        bool RequiereRevision,
        string? MotivoRevision,
        IReadOnlyList<string> CriteriosAplicados,
        IReadOnlyList<string> Explicacion)
    {
    }

    private sealed class ComparadorInterno : IComparer<EvaluacionInterna>
    {
        public static ComparadorInterno Instance { get; } = new();

        public int Compare(EvaluacionInterna? x, EvaluacionInterna? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            for (var i = 0; i < Math.Min(x.Claves.Count, y.Claves.Count); i++)
            {
                var comparacion = x.Claves[i].CompareTo(y.Claves[i]);
                if (comparacion != 0) return comparacion;
            }
            return x.Claves.Count.CompareTo(y.Claves.Count);
        }
    }
}
