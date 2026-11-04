namespace Jardin.Vacantes.Contracts;

public sealed record DatosConfirmacionIngreso(
    Guid Id,
    Guid PeriodoId,
    Guid SolicitudId,
    Guid AspiranteId,
    Guid OfertaId,
    Guid TurnoId,
    int Sala,
    Guid PropuestaId,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    int Revision);
