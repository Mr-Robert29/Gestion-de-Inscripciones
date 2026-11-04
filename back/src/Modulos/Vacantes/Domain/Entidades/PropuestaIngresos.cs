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
    public Guid? PropuestaAnteriorId { get; private set; }
    public EstadoPropuestaIngreso Estado { get; private set; }
    public int Revision { get; private set; }

    private PropuestaIngresos() { }

    public PropuestaIngresos(
        Guid periodoId,
        Guid institucionId,
        Guid usuarioId,
        string motivo,
        string huellaEntradas,
        string entradas,
        string resultados,
        Guid? propuestaAnteriorId = null)
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
        PropuestaAnteriorId = propuestaAnteriorId;
        Estado = EstadoPropuestaIngreso.Borrador;
        Revision = 1;
    }

    public void CambiarEstado(EstadoPropuestaIngreso estado, int revisionEsperada)
    {
        if (revisionEsperada != Revision)
            throw new InvalidOperationException("La propuesta cambió mientras se la estaba editando.");
        if (Estado == estado) return;
        if (Estado == EstadoPropuestaIngreso.Confirmada && estado != EstadoPropuestaIngreso.Confirmada)
            throw new InvalidOperationException("Una propuesta confirmada no puede volver a editarse.");
        Estado = estado;
        Revision++;
    }
}
