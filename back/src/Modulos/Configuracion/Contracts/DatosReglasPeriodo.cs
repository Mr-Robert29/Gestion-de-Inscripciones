namespace Jardin.Configuracion.Contracts;

// Perfil del prototipo: describe reglas, no ejecuta ordenamientos ni sorteos.
public sealed record DatosReglasPeriodo(
    string Perfil,
    DateOnly FechaCorte,
    IReadOnlyList<int> OrdenGruposEtarios,
    IReadOnlyList<string> OrdenProcedimiento,
    string ProcedimientoSorteo,
    string ReglaDistancia,
    IReadOnlyList<int[]> CombinacionesEdades,
    int CapacidadReferenciaSeccion);
