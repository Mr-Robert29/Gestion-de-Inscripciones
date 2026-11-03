namespace Jardin.Configuracion.Domain.Entidades;

// Espacio físico del jardín, independiente de la composición de las futuras secciones.
public sealed class Aula
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public string Nombre { get; private set; } = "";

    private Aula() { }

    public Aula(Guid institucionId, string nombre)
    {
        if (institucionId == Guid.Empty) throw new ArgumentException("La institución es obligatoria.", nameof(institucionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        Nombre = nombre.Trim();
    }
}
