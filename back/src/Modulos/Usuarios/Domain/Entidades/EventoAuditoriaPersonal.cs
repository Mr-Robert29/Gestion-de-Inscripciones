namespace Jardin.Usuarios.Domain.Entidades;

public sealed class EventoAuditoriaPersonal
{
    public Guid Id { get; private set; }
    public Guid ActorUsuarioId { get; private set; }
    public Guid? UsuarioAfectadoId { get; private set; }
    public Guid? InstitucionId { get; private set; }
    public string Accion { get; private set; } = "";
    public string Detalle { get; private set; } = "";
    public DateTime FechaUtc { get; private set; }

    private EventoAuditoriaPersonal() { }

    public EventoAuditoriaPersonal(Guid actorUsuarioId, Guid? usuarioAfectadoId, Guid? institucionId, string accion, string detalle)
    {
        Id = Guid.NewGuid();
        ActorUsuarioId = actorUsuarioId;
        UsuarioAfectadoId = usuarioAfectadoId;
        InstitucionId = institucionId;
        Accion = accion;
        Detalle = detalle;
        FechaUtc = DateTime.UtcNow;
    }
}
