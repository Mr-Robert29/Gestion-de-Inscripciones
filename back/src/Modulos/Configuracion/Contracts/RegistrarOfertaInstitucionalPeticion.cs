namespace Jardin.Configuracion.Contracts;

public sealed record RegistrarOfertaInstitucionalPeticion(
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    string? ReferenciaCapacidad,
    string Motivo);
