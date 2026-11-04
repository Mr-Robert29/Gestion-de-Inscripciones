namespace Jardin.Vacantes.Domain.Entidades;

// Una propuesta es una instantánea histórica; nunca representa ingresos confirmados.
public sealed class PropuestaIngresos
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string Motivo { get; private set; } = "";
    public string HuellaEntradas { get; private set; } = "";
    public string Entradas { get; private set; } = "";
    public string Resultados { get; private set; } = "";

    private PropuestaIngresos() { }

    public PropuestaIngresos(
        Guid periodoId,
        Guid institucionId,
        Guid usuarioId,
        string motivo,
        string huellaEntradas,
        string entradas,
        string resultados)
    {
        if (periodoId == Guid.Empty || institucionId == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("La propuesta requiere período, institución y autor.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        InstitucionId = institucionId;
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Motivo = motivo.Trim();
        HuellaEntradas = huellaEntradas;
        Entradas = entradas;
        Resultados = resultados;
    }
}
