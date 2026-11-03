namespace Jardin.Secciones.Domain;

public static class ReglasSeccion
{
    public const int CapacidadInicialReferencia = 25;

    public const int EdadMinimaPermitida = 3;
    public const int EdadMaximaPermitida = 5;

    public static readonly IReadOnlyList<IReadOnlyList<int>> CombinacionesAprobadasPorDefecto =
    [
        [3],
        [4],
        [5],
        [3, 4],
        [4, 5]
    ];

    public static bool EsCombinacionPermitida(
        IEnumerable<int> edades,
        IEnumerable<IEnumerable<int>>? combinacionesConfiguradas = null)
    {
        var lista = edades.Distinct().OrderBy(e => e).ToList();
        if (lista.Count == 0)
        {
            return false;
        }

        var combinaciones = combinacionesConfiguradas?
            .Select(c => c.Distinct().OrderBy(e => e).ToList())
            .ToList()
            ?? CombinacionesAprobadasPorDefecto.Select(c => c.ToList()).ToList();

        return combinaciones.Any(c => c.SequenceEqual(lista));
    }
}
