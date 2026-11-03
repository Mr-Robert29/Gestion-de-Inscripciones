namespace Jardin.Inscripciones.Domain.Entidades;

// Delimita el catálogo institucional sin copiar los datos personales en cada solicitud.
public sealed class RegistroAspiranteInstitucion
{
    public Guid InstitucionId { get; private set; }
    public Guid AspiranteId { get; private set; }
    private RegistroAspiranteInstitucion() { }
    public RegistroAspiranteInstitucion(Guid institucionId, Guid personaId)
    {
        if (institucionId == Guid.Empty || personaId == Guid.Empty) throw new ArgumentException("Institución y persona son obligatorias.");
        InstitucionId = institucionId;
        AspiranteId = personaId;
    }
}
