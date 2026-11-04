namespace Jardin.Secciones.Domain.Entidades;

// Impide que una distribución leída antes de una baja cree después una asignación activa.
public sealed class IngresoRetiradoSecciones
{
    public Guid IngresoId { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Motivo { get; private set; } = "";
    private IngresoRetiradoSecciones() { }
    public IngresoRetiradoSecciones(
        Guid ingresoId,
        Guid periodoId,
        Guid usuarioId,
        string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        IngresoId = ingresoId;
        PeriodoId = periodoId;
        UsuarioId = usuarioId;
        Motivo = motivo;
        Fecha = DateTimeOffset.UtcNow;
    }
}
