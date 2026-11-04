namespace Jardin.Vacantes.Domain.Entidades;

public sealed class IngresoConfirmado
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public Guid AspiranteId { get; private set; }
    public Guid OfertaId { get; private set; }
    public Guid TurnoId { get; private set; }
    public int Sala { get; private set; }
    public Guid PropuestaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public int Revision { get; private set; }

    private IngresoConfirmado() { }

    public IngresoConfirmado(
        Guid periodoId,
        Guid solicitudId,
        Guid aspiranteId,
        Guid ofertaId,
        Guid turnoId,
        int sala,
        Guid propuestaId,
        Guid usuarioId)
    {
        if (periodoId == Guid.Empty || solicitudId == Guid.Empty || aspiranteId == Guid.Empty || ofertaId == Guid.Empty || turnoId == Guid.Empty || propuestaId == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("El ingreso requiere todas sus referencias.");
        if (sala is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(sala));
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        SolicitudId = solicitudId;
        AspiranteId = aspiranteId;
        OfertaId = ofertaId;
        TurnoId = turnoId;
        Sala = sala;
        PropuestaId = propuestaId;
        UsuarioId = usuarioId;
        Fecha = DateTimeOffset.UtcNow;
        Revision = 1;
    }
}
