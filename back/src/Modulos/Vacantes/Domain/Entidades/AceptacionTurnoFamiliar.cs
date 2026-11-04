namespace Jardin.Vacantes.Domain.Entidades;

public sealed class AceptacionTurnoFamiliar
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid PropuestaId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public Guid TurnoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string? Referencia { get; private set; }

    private AceptacionTurnoFamiliar() { }

    public AceptacionTurnoFamiliar(
        Guid periodoId,
        Guid propuestaId,
        Guid solicitudId,
        Guid turnoId,
        Guid usuarioId,
        string? referencia)
    {
        if (periodoId == Guid.Empty || propuestaId == Guid.Empty || solicitudId == Guid.Empty || turnoId == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("La aceptación requiere período, propuesta, solicitud, turno y usuario.");
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        PropuestaId = propuestaId;
        SolicitudId = solicitudId;
        TurnoId = turnoId;
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
    }
}
