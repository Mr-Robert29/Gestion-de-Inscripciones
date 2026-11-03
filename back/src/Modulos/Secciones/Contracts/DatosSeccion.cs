namespace Jardin.Secciones.Contracts;

public sealed record DatosSeccion(
    Guid Id,
    Guid PeriodoId,
    Guid AulaId,
    Guid TurnoId,
    string Nombre,
    string Tipo,
    IReadOnlyList<int> EdadesAdmitidas,
    int Capacidad,
    int Revision,
    bool Activa);
