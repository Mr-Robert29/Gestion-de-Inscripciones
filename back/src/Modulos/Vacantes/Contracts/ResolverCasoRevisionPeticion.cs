namespace Jardin.Vacantes.Contracts;

public sealed record ResolverCasoRevisionPeticion(
    IReadOnlyList<Guid> OrdenDecidido,
    string Motivo,
    int Revision);

public sealed record ReabrirCasoRevisionPeticion(
    string Motivo,
    int Revision);
