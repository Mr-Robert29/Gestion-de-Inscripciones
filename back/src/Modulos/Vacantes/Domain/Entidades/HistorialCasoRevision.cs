namespace Jardin.Vacantes.Domain.Entidades;

public sealed class HistorialCasoRevision
{
    public Guid Id { get; private set; }
    public Guid CasoRevisionId { get; private set; }
    public string Accion { get; private set; } = "";
    public EstadoCasoRevision EstadoAnterior { get; private set; }
    public EstadoCasoRevision EstadoNuevo { get; private set; }
    public string OrdenAnterior { get; private set; } = "[]";
    public string OrdenNueva { get; private set; } = "[]";
    public string Motivo { get; private set; } = "";
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }

    private HistorialCasoRevision() { }

    public HistorialCasoRevision(
        CasoRevision caso,
        string accion,
        Guid usuarioId,
        string motivo,
        IReadOnlyList<Guid> ordenAnterior,
        IReadOnlyList<Guid> ordenNueva,
        EstadoCasoRevision estadoAnterior,
        EstadoCasoRevision estadoNuevo)
    {
        if (caso.Id == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("El caso y el usuario son obligatorios.");
        ArgumentException.ThrowIfNullOrWhiteSpace(accion);
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        Id = Guid.NewGuid();
        CasoRevisionId = caso.Id;
        Accion = accion.Trim();
        EstadoAnterior = estadoAnterior;
        EstadoNuevo = estadoNuevo;
        OrdenAnterior = System.Text.Json.JsonSerializer.Serialize(ordenAnterior);
        OrdenNueva = System.Text.Json.JsonSerializer.Serialize(ordenNueva);
        Motivo = motivo.Trim();
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
    }
}
