namespace Jardin.Secciones.Contracts;

public sealed record ModificarAsignacionPropuestaPeticion(
    Guid IngresoId,
    Guid? SeccionId,
    string? MotivoSinSeccion,
    string Motivo);
