namespace Jardin.Inscripciones.Domain.Entidades;

// Auditoría propia del módulo, consultable solo con permiso institucional.
public sealed class CambioInscripciones
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid EntidadId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Accion { get; private set; } = "";
    public string Datos { get; private set; } = "";
    private CambioInscripciones() { }
    public CambioInscripciones(
        Guid institucionId,
        Guid usuarioId,
        Guid entidadId,
        string accion,
        string datos)
    {
        if (institucionId == Guid.Empty || usuarioId == Guid.Empty || entidadId == Guid.Empty)
            throw new ArgumentException("La institución, el usuario y la entidad son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(accion);
        ArgumentException.ThrowIfNullOrWhiteSpace(datos);
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        UsuarioId = usuarioId;
        EntidadId = entidadId;
        Fecha = DateTimeOffset.UtcNow;
        Accion = accion;
        Datos = datos;
    }
}
