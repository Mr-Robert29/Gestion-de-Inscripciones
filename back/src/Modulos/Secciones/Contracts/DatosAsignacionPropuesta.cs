namespace Jardin.Secciones.Contracts;

public sealed record DatosAsignacionPropuesta(
    Guid Id,
    Guid IngresoId,
    Guid? SeccionId,
    int Edad,
    Guid TurnoId,
    string? MotivoSinSeccion,
    Guid? ModificadaPor,
    DateTimeOffset? ModificadaEn,
    string? MotivoModificacion);
