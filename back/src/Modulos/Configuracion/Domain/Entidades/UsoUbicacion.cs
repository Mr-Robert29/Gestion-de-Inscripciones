namespace Jardin.Configuracion.Domain.Entidades;

// Índice de dependencias: el contenido y las decisiones siguen perteneciendo al módulo de origen.
public sealed class UsoUbicacion
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid ReferenciaId { get; private set; }
    public string Tipo { get; private set; } = "";
    public int RevisionUbicacion { get; private set; }

    private UsoUbicacion() { }

    public UsoUbicacion(
        Guid institucionId,
        Guid referenciaId,
        string tipo,
        int revisionUbicacion)
    {
        if (referenciaId == Guid.Empty) throw new ArgumentException("La referencia es obligatoria.", nameof(referenciaId));
        if (tipo is not ("MedicionDistancia" or "PropuestaIngreso" or "PropuestaDistribucion"))
            throw new ArgumentException("Tipo de referencia desconocido.", nameof(tipo));
        if (revisionUbicacion < 1) throw new ArgumentOutOfRangeException(nameof(revisionUbicacion));
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        ReferenciaId = referenciaId;
        Tipo = tipo;
        RevisionUbicacion = revisionUbicacion;
    }
}
