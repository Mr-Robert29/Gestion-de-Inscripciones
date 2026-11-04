namespace Jardin.Vacantes.Domain.Entidades;

public sealed class IngresoConfirmado
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public Guid AspiranteId { get; private set; }
    public int Edad { get; private set; }
    public Guid OfertaId { get; private set; }
    public Guid TurnoId { get; private set; }
    public int Sala { get; private set; }
    public Guid PropuestaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public int Revision { get; private set; }
    public bool Activo { get; private set; } = true;
    public bool BajaEnProceso { get; private set; }
    public Guid? UsuarioBajaId { get; private set; }
    public DateTimeOffset? FechaSolicitudBaja { get; private set; }
    public DateTimeOffset? FechaBaja { get; private set; }
    public string? MotivoBaja { get; private set; }
    public Guid? OfrecimientoId { get; private set; }

    private IngresoConfirmado() { }

    public IngresoConfirmado(
        Guid periodoId,
        Guid solicitudId,
        Guid aspiranteId,
        int edad,
        Guid ofertaId,
        Guid turnoId,
        int sala,
        Guid propuestaId,
        Guid usuarioId,
        Guid? ofrecimientoId = null)
    {
        if (periodoId == Guid.Empty || solicitudId == Guid.Empty || aspiranteId == Guid.Empty || ofertaId == Guid.Empty || turnoId == Guid.Empty || propuestaId == Guid.Empty || usuarioId == Guid.Empty)
            throw new ArgumentException("El ingreso requiere todas sus referencias.");
        if (sala is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(sala));
        if (edad is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(edad));
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        SolicitudId = solicitudId;
        AspiranteId = aspiranteId;
        Edad = edad;
        OfertaId = ofertaId;
        TurnoId = turnoId;
        Sala = sala;
        PropuestaId = propuestaId;
        UsuarioId = usuarioId;
        OfrecimientoId = ofrecimientoId;
        Fecha = DateTimeOffset.UtcNow;
        Revision = 1;
    }

    public void SolicitarBaja(Guid usuarioId, string motivo)
    {
        if (!Activo) throw new InvalidOperationException("El ingreso ya está dado de baja.");
        if (BajaEnProceso) return;
        if (usuarioId == Guid.Empty) throw new ArgumentException("Falta el autor de la baja.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        BajaEnProceso = true;
        UsuarioBajaId = usuarioId;
        MotivoBaja = motivo.Trim();
        FechaSolicitudBaja = DateTimeOffset.UtcNow;
        Revision++;
    }

    public void CompletarBaja()
    {
        if (!Activo) return;
        if (!BajaEnProceso) throw new InvalidOperationException("La baja no fue solicitada.");
        Activo = false;
        BajaEnProceso = false;
        FechaBaja = DateTimeOffset.UtcNow;
        Revision++;
    }
}
