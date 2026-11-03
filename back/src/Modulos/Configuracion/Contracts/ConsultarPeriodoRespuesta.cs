namespace Jardin.Configuracion.Contracts;

// Salida con fechas, estado y revisión de edición.
public sealed record ConsultarPeriodoRespuesta(
    Guid Id,
    Guid InstitucionId,
    int Ciclo,
    DateOnly Inicio,
    DateOnly Fin,
    string Estado,
    int Revision,
    Guid? VersionReglasId = null,
    Guid? VersionOfertaId = null);
