namespace Jardin.Configuracion.Contracts;

public sealed record ActualizarOfertaInstitucionalPeticion(
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    string? ReferenciaCapacidad,
    string Motivo);
