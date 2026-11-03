namespace Jardin.Secciones.Contracts;

public sealed record CambiarComposicionSeccionPeticion(
    IReadOnlyList<int> NuevasEdades,
    string Motivo);
