namespace Jardin.Configuracion.Contracts;
public sealed record ConsultarAulaRespuesta(
    Guid Id,
    Guid InstitucionId,
    string Nombre);
