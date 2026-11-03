namespace Jardin.Secciones.Contracts;

public sealed record DefinirSeccionPeticion(
    Guid PeriodoId,
    Guid AulaId,
    Guid TurnoId,
    string Nombre,
    IReadOnlyList<int> EdadesAdmitidas);
