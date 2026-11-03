namespace Jardin.Inscripciones.Domain.Entidades;

// Solo representa un turno expresamente aceptado; el orden de asignación sigue pendiente de definición.
public sealed class TurnoAlternativoSolicitud
{
    public Guid TurnoId { get; private set; }
    private TurnoAlternativoSolicitud() { }
    internal TurnoAlternativoSolicitud(Guid turnoId) { TurnoId = turnoId; }
}
