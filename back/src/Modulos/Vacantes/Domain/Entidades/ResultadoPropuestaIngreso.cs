namespace Jardin.Vacantes.Domain.Entidades;

public sealed record ResultadoPropuestaIngreso(
    Guid SolicitudId,
    Guid AspiranteId,
    int? Sala,
    string Estado,
    Guid? OfertaId,
    Guid? TurnoId,
    bool EsTurnoAlternativo,
    string Explicacion);
