namespace Jardin.Configuracion.Contracts;

// Datos contiene la instantánea JSON del formato indicado, nunca datos actuales recalculados.
public sealed record ConsultarVersionRespuesta(
    Guid Id,
    Guid PeriodoId,
    string Tipo,
    int Formato,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string Referencia,
    string Motivo,
    System.Text.Json.JsonElement Datos);
