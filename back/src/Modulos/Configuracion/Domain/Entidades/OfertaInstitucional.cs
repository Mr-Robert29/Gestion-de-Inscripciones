namespace Jardin.Configuracion.Domain.Entidades;

// Plantilla reutilizable de aula, turno y capacidad de referencia de una institución.
public sealed class OfertaInstitucional
{
    public Guid Id { get; private set; }
    public Guid InstitucionId { get; private set; }
    public Guid AulaId { get; private set; }
    public Guid TurnoId { get; private set; }
    public int Capacidad { get; private set; }
    public string ReferenciaCapacidad { get; private set; } = "";

    private OfertaInstitucional() { }

    public OfertaInstitucional(
        Guid institucionId,
        Guid aulaId,
        Guid turnoId,
        int capacidad,
        string? referenciaCapacidad)
    {
        if (institucionId == Guid.Empty || aulaId == Guid.Empty || turnoId == Guid.Empty)
            throw new ArgumentException("Institución, aula y turno son obligatorios.");
        if (capacidad < 0)
            throw new ArgumentOutOfRangeException(nameof(capacidad), "La capacidad no puede ser negativa.");

        Id = Guid.NewGuid();
        InstitucionId = institucionId;
        AulaId = aulaId;
        TurnoId = turnoId;
        Capacidad = capacidad;
        ReferenciaCapacidad = string.IsNullOrWhiteSpace(referenciaCapacidad)
            ? "Sin referencia informada"
            : referenciaCapacidad.Trim();
    }

    public void Actualizar(Guid aulaId, Guid turnoId, int capacidad, string? referenciaCapacidad)
    {
        if (aulaId == Guid.Empty || turnoId == Guid.Empty)
            throw new ArgumentException("Aula y turno son obligatorios.");
        if (capacidad < 0)
            throw new ArgumentOutOfRangeException(nameof(capacidad), "La capacidad no puede ser negativa.");

        AulaId = aulaId;
        TurnoId = turnoId;
        Capacidad = capacidad;
        ReferenciaCapacidad = string.IsNullOrWhiteSpace(referenciaCapacidad)
            ? "Sin referencia informada"
            : referenciaCapacidad.Trim();
    }
}
