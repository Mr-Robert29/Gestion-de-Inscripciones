namespace Jardin.Configuracion.Domain.Entidades;

// Identifica un turno del jardín sin imponer nombres u horarios todavía no acordados.
public sealed class Turno
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public string Nombre { get; private set; } = "";

    private Turno() { }

    public Turno(Guid institucionId, string nombre)
    {
        if (institucionId == Guid.Empty) throw new ArgumentException("La institución es obligatoria.", nameof(institucionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        Nombre = nombre.Trim();
    }
}
