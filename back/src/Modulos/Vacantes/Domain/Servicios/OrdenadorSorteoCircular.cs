namespace Jardin.Vacantes.Domain.Servicios;

using Jardin.Vacantes.Domain.Entidades;

public static class OrdenadorSorteoCircular
{
    public static IReadOnlyList<ResultadoOrdenSorteo> Ordenar(
        int numeroSorteo,
        IEnumerable<ParticipanteSorteo> participantes)
    {
        Sorteo.ValidarNumero(numeroSorteo);
        ArgumentNullException.ThrowIfNull(participantes);

        var entradas = participantes.ToList();
        if (entradas.Any(x => x.Sala is < 3 or > 5))
            throw new ArgumentOutOfRangeException(nameof(participantes), "Todos los participantes deben pertenecer a una sala válida.");
        if (entradas.Select(x => x.SolicitudId).Distinct().Count() != entradas.Count)
            throw new InvalidOperationException("Una solicitud no puede participar más de una vez en el mismo sorteo.");

        var conDni = entradas
            .Where(x => x.TerminacionDni is not null)
            .Select(x => new
            {
                Participante = x,
                Terminacion = x.TerminacionDni!.Value,
                Clave = (x.TerminacionDni.Value - numeroSorteo + 1000) % 1000
            })
            .OrderBy(x => x.Clave)
            .ThenBy(x => x.Participante.SolicitudId)
            .ToList();

        var clavesRepetidas = conDni
            .GroupBy(x => x.Clave)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet();

        var resultados = new List<ResultadoOrdenSorteo>(entradas.Count);
        for (var indice = 0; indice < conDni.Count; indice++)
        {
            var entrada = conDni[indice];
            var requiereRevision = clavesRepetidas.Contains(entrada.Clave);
            resultados.Add(new ResultadoOrdenSorteo(
                entrada.Participante.SolicitudId,
                entrada.Participante.AspiranteId,
                entrada.Participante.Sala,
                entrada.Terminacion,
                entrada.Clave,
                requiereRevision ? null : indice + 1,
                requiereRevision,
                requiereRevision ? "La terminación de DNI coincide con otro participante." : null));
        }

        resultados.AddRange(entradas
            .Where(x => x.TerminacionDni is null)
            .OrderBy(x => x.SolicitudId)
            .Select(x => new ResultadoOrdenSorteo(
                x.SolicitudId,
                x.AspiranteId,
                x.Sala,
                null,
                null,
                null,
                true,
                "El participante no tiene DNI para calcular el orden circular.")));

        return resultados;
    }
}
