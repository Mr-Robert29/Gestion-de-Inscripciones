namespace Jardin.Vacantes.Contracts;

public sealed record DatosIngresoHistorial(
    Guid Id,
    Guid PeriodoId,
    Guid SolicitudId,
    Guid AspiranteId,
    int Edad,
    Guid OfertaId,
    Guid TurnoId,
    int Sala,
    Guid PropuestaId,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    int Revision,
    bool Activo,
    bool BajaEnProceso,
    Guid? UsuarioBajaId,
    DateTimeOffset? FechaSolicitudBaja,
    DateTimeOffset? FechaBaja,
    string? MotivoBaja,
    Guid? OfrecimientoId);
