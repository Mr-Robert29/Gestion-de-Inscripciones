namespace Jardin.Usuarios.Contracts;

public sealed record DatosAuditoriaPersonal(
    Guid Id,
    Guid ActorUsuarioId,
    Guid? UsuarioAfectadoId,
    Guid? InstitucionId,
    string Accion,
    string Detalle,
    DateTime FechaUtc);
