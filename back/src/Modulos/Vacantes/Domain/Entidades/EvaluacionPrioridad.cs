namespace Jardin.Vacantes.Domain.Entidades;

public sealed class EvaluacionPrioridad
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid VersionReglasId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public int RevisionPeriodo { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Datos { get; private set; } = "";

    private EvaluacionPrioridad() { }

    public EvaluacionPrioridad(
        Guid periodoId,
        Guid versionReglasId,
        Guid usuarioId,
        int revisionPeriodo,
        string datos)
    {
        if (periodoId == Guid.Empty || versionReglasId == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("Período, versión de reglas y usuario son obligatorios.");
        if (revisionPeriodo < 1) throw new ArgumentOutOfRangeException(nameof(revisionPeriodo));
        ArgumentException.ThrowIfNullOrWhiteSpace(datos);
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        VersionReglasId = versionReglasId;
        UsuarioId = usuarioId;
        RevisionPeriodo = revisionPeriodo;
        Fecha = DateTimeOffset.UtcNow;
        Datos = datos;
    }
}
