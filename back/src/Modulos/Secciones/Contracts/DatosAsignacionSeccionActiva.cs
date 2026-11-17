namespace Jardin.Secciones.Contracts;

public sealed record DatosAsignacionSeccionActiva(
    Guid IngresoId,
    Guid SolicitudId,
    int Edad,
    int Sala,
    Guid TurnoId,
    string NombreSeccion);
