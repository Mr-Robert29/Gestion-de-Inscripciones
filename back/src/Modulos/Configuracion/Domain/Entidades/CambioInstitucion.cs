namespace Jardin.Configuracion.Domain.Entidades;

// Registro de historial: nunca se modifica una entrada existente.
public sealed class CambioInstitucion
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Accion { get; private set; } = "";
    public string Motivo { get; private set; } = "";
    public string? DatosAnteriores { get; private set; }
    public string DatosNuevos { get; private set; } = "";

    private CambioInstitucion() { }

    public CambioInstitucion(
        Guid institucionId,
        Guid usuarioId,
        string accion,
        string motivo,
        string? datosAnteriores,
        string datosNuevos)
    {
        if (usuarioId == Guid.Empty) throw new ArgumentException("El autor es obligatorio.", nameof(usuarioId));
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Accion = accion;
        Motivo = motivo;
        DatosAnteriores = datosAnteriores;
        DatosNuevos = datosNuevos;
    }
}
