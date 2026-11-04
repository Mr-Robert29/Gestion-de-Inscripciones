namespace Jardin.Vacantes.Domain.Entidades;

// Una reserva convertida en ingreso conserva el mismo registro: no consume dos plazas.
public sealed class OcupacionPlaza
{
    public Guid Id { get; private set; }
    public Guid PeriodoId { get; private set; }
    public Guid AspiranteId { get; private set; }
    public Guid OfertaId { get; private set; }
    public int Sala { get; private set; }
    public string Tipo { get; private set; } = "";
    public bool Activa { get; private set; }
    public int Revision { get; private set; }

    private OcupacionPlaza() { }

    public OcupacionPlaza(
        Guid periodoId,
        Guid aspiranteId,
        Guid ofertaId,
        int sala,
        string tipo)
    {
        if (periodoId == Guid.Empty || aspiranteId == Guid.Empty || ofertaId == Guid.Empty)
            throw new ArgumentException("La ocupación requiere período, aspirante y oferta.");
        if (sala is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(sala));
        ValidarTipo(tipo);
        Id = Guid.NewGuid();
        PeriodoId = periodoId;
        AspiranteId = aspiranteId;
        OfertaId = ofertaId;
        Sala = sala;
        Tipo = tipo;
        Activa = true;
        Revision = 1;
    }

    public void ConfirmarIngreso()
    {
        if (!Activa) throw new InvalidOperationException("La ocupación está inactiva.");
        if (Tipo == "Ingreso") return;
        Tipo = "Ingreso";
        Revision++;
    }

    public void Liberar()
    {
        if (!Activa) return;
        Activa = false;
        Revision++;
    }

    private static void ValidarTipo(string tipo)
    {
        if (tipo is not ("Ingreso" or "Reserva"))
            throw new ArgumentException("El tipo debe ser Ingreso o Reserva.", nameof(tipo));
    }
}
