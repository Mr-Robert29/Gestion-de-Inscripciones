namespace Jardin.Secciones.Domain.Entidades;

// Instantánea del cambio: las posteriores modificaciones no alteran este antecedente.
public sealed class CambioComposicionSeccion
{
    public Guid Id { get; private set; }
    public Guid SeccionId { get; private set; }
    public Guid PeriodoId { get; private set; }
    public string EdadesAnteriores { get; private set; } = "";
    public string EdadesNuevas { get; private set; } = "";
    public int RevisionAnterior { get; private set; }
    public int RevisionNueva { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Motivo { get; private set; } = "";
    private CambioComposicionSeccion() { }
    public CambioComposicionSeccion(
        Seccion seccion,
        IReadOnlyList<int> anteriores,
        int revisionAnterior,
        Guid usuarioId,
        string motivo)
    {
        if (usuarioId == Guid.Empty) throw new ArgumentException("El usuario no puede estar vacío.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        Id = Guid.NewGuid();
        SeccionId = seccion.Id;
        PeriodoId = seccion.PeriodoId;
        EdadesAnteriores = string.Join(",", anteriores.Order());
        EdadesNuevas = string.Join(",", seccion.EdadesAdmitidas.Order());
        RevisionAnterior = revisionAnterior;
        RevisionNueva = seccion.Revision;
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Motivo = motivo.Trim();
    }
}
