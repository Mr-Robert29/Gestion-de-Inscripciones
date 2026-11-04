namespace Jardin.Secciones.Contracts;

public sealed record CambiarComposicionSeccionPeticion(
    IReadOnlyList<int> NuevasEdades,
    string Motivo,
    int Revision = 0);
