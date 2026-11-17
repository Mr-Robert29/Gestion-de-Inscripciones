namespace Jardin.Vacantes.Contracts;

public sealed record RegistrarAceptacionesTurnoLotePeticion(
    IReadOnlyList<Guid> SolicitudIds,
    string ReferenciaComun,
    bool ConfirmaAceptacionesFamiliares);
