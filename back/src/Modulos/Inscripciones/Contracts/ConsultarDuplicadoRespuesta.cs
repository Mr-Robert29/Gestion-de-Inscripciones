namespace Jardin.Inscripciones.Contracts;

// DatosEvaluados conserva la comparación; no representa una decisión automática de ingreso.
public sealed record ConsultarDuplicadoRespuesta(
    Guid Id,
    Guid InstitucionId,
    Guid PeriodoId,
    Guid SolicitudAId,
    Guid SolicitudBId,
    string PerfilDeteccion,
    IReadOnlyList<string> Criterios,
    System.Text.Json.JsonElement DatosEvaluados,
    string Estado,
    int Revision,
    Guid UsuarioDeteccionId,
    DateTimeOffset FechaDeteccion,
    Guid UsuarioUltimaDeteccionId,
    DateTimeOffset FechaUltimaDeteccion,
    Guid? UsuarioResolucionId,
    DateTimeOffset? FechaResolucion,
    string? MotivoResolucion);
