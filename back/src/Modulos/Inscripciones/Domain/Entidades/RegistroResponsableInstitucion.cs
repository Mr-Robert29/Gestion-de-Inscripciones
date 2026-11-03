namespace Jardin.Inscripciones.Domain.Entidades;

// Delimita el catálogo institucional sin copiar los datos personales en cada solicitud.
public sealed class RegistroResponsableInstitucion
{
    public Guid InstitucionId { get; private set; }
    public Guid ResponsableId { get; private set; }
    private RegistroResponsableInstitucion() { }
    public RegistroResponsableInstitucion(Guid institucionId, Guid personaId)
    {
        if (institucionId == Guid.Empty || personaId == Guid.Empty) throw new ArgumentException("Institución y persona son obligatorias.");
        InstitucionId = institucionId;
        ResponsableId = personaId;
    }
}
