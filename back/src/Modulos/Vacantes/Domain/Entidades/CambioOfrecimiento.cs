namespace Jardin.Vacantes.Domain.Entidades;

public sealed class CambioOfrecimiento
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OfrecimientoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; } = DateTimeOffset.UtcNow;
    public string EstadoAnterior { get; private set; } = "";
    public string EstadoNuevo { get; private set; } = "";
    public string Motivo { get; private set; } = "";
    public string? Referencia { get; private set; }
    private CambioOfrecimiento() { }
    public CambioOfrecimiento(
        Guid ofrecimientoId,
        Guid usuarioId,
        string estadoAnterior,
        string estadoNuevo,
        string motivo,
        string? referencia = null)
    {
        OfrecimientoId = ofrecimientoId;
        UsuarioId = usuarioId;
        EstadoAnterior = estadoAnterior;
        EstadoNuevo = estadoNuevo;
        Motivo = motivo;
        Referencia = referencia;
    }
}
