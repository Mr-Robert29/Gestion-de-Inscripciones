namespace Jardin.Vacantes.Contracts;

public sealed record DatosHistorialCasoRevision(
    Guid Id,
    Guid CasoRevisionId,
    string Accion,
    string EstadoAnterior,
    string EstadoNuevo,
    IReadOnlyList<Guid> OrdenAnterior,
    IReadOnlyList<Guid> OrdenNueva,
    string Motivo,
    Guid UsuarioId,
    DateTimeOffset Fecha);
