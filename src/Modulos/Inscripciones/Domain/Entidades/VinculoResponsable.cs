namespace Jardin.Inscripciones.Domain.Entidades;

public sealed class VinculoResponsable
{
    public Guid AspiranteId { get; private set; }
    public Guid ResponsableId { get; private set; }
    public string TipoVinculo { get; private set; } = "";
    public bool ContactoPrincipal { get; private set; }
    public bool EsTutor { get; private set; }
    public Responsable Responsable { get; private set; } = null!;
    private VinculoResponsable() { }
    internal VinculoResponsable(Guid aspiranteId, Responsable responsable, string tipoVinculo,
        bool contactoPrincipal, bool esTutor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoVinculo);
        AspiranteId = aspiranteId; ResponsableId = responsable.Id; Responsable = responsable;
        TipoVinculo = tipoVinculo.Trim(); ContactoPrincipal = contactoPrincipal; EsTutor = esTutor;
    }
}
