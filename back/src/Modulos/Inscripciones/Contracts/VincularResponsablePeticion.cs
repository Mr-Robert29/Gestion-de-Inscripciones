namespace Jardin.Inscripciones.Contracts;

public sealed record VincularResponsablePeticion(
    Guid ResponsableId,
    string TipoVinculo,
    bool ContactoPrincipal = false,
    bool EsTutor = false);
