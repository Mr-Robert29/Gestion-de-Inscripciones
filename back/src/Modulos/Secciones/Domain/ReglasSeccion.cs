namespace Jardin.Secciones.Domain;

/// 
/// Reglas de dominio para las secciones: capacidad inicial de referencia y combinaciones de edades aprobadas para el prototipo.
/// 
public static class ReglasSeccion
{
    /// 
    /// Capacidad inicial (25 alumnos por sección) 
    /// 
    public const int CapacidadInicialReferencia = 25;

    /// Edades permitidas para las salas de nivel inicial (Salas de 3, 4 y 5 años).
    /// 
    public const int EdadMinimaPermitida = 3;
    public const int EdadMaximaPermitida = 5;

    /// Combinaciones aprobadas:
    /// Puras: [3], [4], [5]. Multiedad: [3, 4], [4, 5].
    public static readonly IReadOnlyList<IReadOnlyList<int>> CombinacionesAprobadasPorDefecto =
    [
        [3],
        [4],
        [5],
        [3, 4],
        [4, 5]
    ];

    /// Determina si una lista de edades corresponde a una combinación permitida (ya sea configurada por el período o la estándar por defecto).
    
    public static bool EsCombinacionPermitida(
        IEnumerable<int> edades,
        IEnumerable<IEnumerable<int>>? combinacionesConfiguradas = null)
    {
        var lista = edades.Distinct().OrderBy(e => e).ToList();
        if (lista.Count == 0)
        {
            return false;
        }

        // Si se pasan combinaciones explícitas de la configuración del período, se evalúan esas;
        // de lo contrario, se evalúa contra las aprobadas por defecto para el prototipo.
        var combinaciones = combinacionesConfiguradas?
            .Select(c => c.Distinct().OrderBy(e => e).ToList())
            .ToList()
            ?? CombinacionesAprobadasPorDefecto.Select(c => c.ToList()).ToList();

        return combinaciones.Any(c => c.SequenceEqual(lista));
    }
}
