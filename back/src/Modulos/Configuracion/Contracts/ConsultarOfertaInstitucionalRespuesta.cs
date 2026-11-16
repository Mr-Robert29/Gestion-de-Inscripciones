namespace Jardin.Configuracion.Contracts;

public sealed record ConsultarOfertaInstitucionalRespuesta(
    Guid Id,
    Guid InstitucionId,
    Guid AulaId,
    Guid TurnoId,
    int Capacidad,
    string ReferenciaCapacidad);
