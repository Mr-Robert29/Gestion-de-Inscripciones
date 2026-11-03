namespace Jardin.Inscripciones.Contracts;

// Vigente compara las entradas actuales; el origen sigue siendo manual y provisorio.
public sealed record ConsultarDistanciaRespuesta(
    Guid Id,
    Guid SolicitudId,
    int NumeroRegistro,
    decimal DistanciaMetros,
    string Origen,
    string Referencia,
    int RevisionDomicilio,
    int RevisionUbicacionJardin,
    DateTimeOffset FechaRegistro,
    Guid UsuarioId,
    System.Text.Json.JsonElement Entradas,
    bool EsUltimoRegistro,
    bool EsVigente);
