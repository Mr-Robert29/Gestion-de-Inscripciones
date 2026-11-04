namespace Jardin.Vacantes.Domain.Entidades;

// La aceptación familiar conserva la reserva; solamente Confirmar crea el ingreso.
public sealed class OfrecimientoVacante
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OperacionId { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid SolicitudId { get; private set; }
    public Guid AspiranteId { get; private set; }
    public Guid OfertaId { get; private set; }
    public Guid TurnoId { get; private set; }
    public Guid PropuestaListaId { get; private set; }
    public Guid ReservaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; } = DateTimeOffset.UtcNow;
    public string Motivo { get; private set; } = "";
    public string Estado { get; private set; } = "Ofrecido";
    public int Revision { get; private set; } = 1;
    public Guid? UsuarioRespuestaId { get; private set; }
    public DateTimeOffset? FechaRespuesta { get; private set; }
    public string? MotivoRespuesta { get; private set; }
    public string? ReferenciaRespuesta { get; private set; }
    public Guid? IngresoId { get; private set; }
    private OfrecimientoVacante() { }

    public OfrecimientoVacante(
        Guid operacionId,
        Guid periodoId,
        Guid institucionId,
        Guid solicitudId,
        Guid aspiranteId,
        Guid ofertaId,
        Guid turnoId,
        Guid propuestaListaId,
        Guid reservaId,
        Guid usuarioId,
        string motivo)
    {
        if (new[] { operacionId, periodoId, institucionId, solicitudId, aspiranteId, ofertaId, turnoId, propuestaListaId, reservaId, usuarioId }.Any(x => x == Guid.Empty))
            throw new ArgumentException("El ofrecimiento requiere todas sus referencias.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        OperacionId = operacionId;
        PeriodoId = periodoId;
        InstitucionId = institucionId;
        SolicitudId = solicitudId;
        AspiranteId = aspiranteId;
        OfertaId = ofertaId;
        TurnoId = turnoId;
        PropuestaListaId = propuestaListaId;
        ReservaId = reservaId;
        UsuarioId = usuarioId;
        Motivo = motivo.Trim();
    }

    public void Responder(
        string respuesta,
        int revisionEsperada,
        Guid usuarioId,
        string motivo,
        string? referencia)
    {
        if (respuesta is not ("Aceptado" or "Rechazado" or "Cancelado")) throw new ArgumentException("La respuesta no es válida.");
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        if (respuesta == "Aceptado") ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        if (usuarioId == Guid.Empty) throw new ArgumentException("Falta el autor de la respuesta.");
        if ((Estado == respuesta || Estado == "Confirmado" && respuesta == "Aceptado") &&
            MotivoRespuesta == motivo.Trim() && ReferenciaRespuesta == referencia?.Trim()) return;
        if (Revision != revisionEsperada) throw new InvalidOperationException("El ofrecimiento cambió. Volvé a consultarlo.");
        if (Estado is not ("Ofrecido" or "Aceptado")) throw new InvalidOperationException("El ofrecimiento ya está cerrado.");
        if (Estado == "Aceptado" && respuesta != "Cancelado") throw new InvalidOperationException("Una aceptación solo puede cancelarse antes de confirmar.");
        Estado = respuesta;
        MotivoRespuesta = motivo.Trim();
        ReferenciaRespuesta = referencia?.Trim();
        UsuarioRespuestaId = usuarioId;
        FechaRespuesta = DateTimeOffset.UtcNow;
        Revision++;
    }

    public void Confirmar(Guid ingresoId, int revisionEsperada)
    {
        if (ingresoId == Guid.Empty) throw new ArgumentException("Falta el ingreso confirmado.");
        if (Estado == "Confirmado" && IngresoId == ingresoId) return;
        if (Revision != revisionEsperada) throw new InvalidOperationException("El ofrecimiento cambió. Volvé a consultarlo.");
        if (Estado != "Aceptado") throw new InvalidOperationException("Debe registrarse la aceptación familiar antes de confirmar.");
        IngresoId = ingresoId;
        Estado = "Confirmado";
        Revision++;
    }
}
