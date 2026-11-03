namespace Jardin.Inscripciones.Contracts;

public sealed record ResolverDuplicadoPeticion(
    string Decision,
    int Revision,
    string Motivo);
