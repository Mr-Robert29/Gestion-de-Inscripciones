namespace Jardin.Vacantes.Contracts;

public sealed record ModificarPropuestaIngresosPeticion(
    string HuellaEsperada,
    string Motivo,
    IReadOnlyList<ModificarResultadoPropuestaPeticion> Cambios);

public sealed record ModificarResultadoPropuestaPeticion(
    Guid SolicitudId,
    string Estado,
    Guid? OfertaId,
    Guid? TurnoId,
    bool EsTurnoAlternativo,
    string Explicacion);

public sealed record RegistrarAceptacionTurnoPeticion(
    Guid SolicitudId,
    Guid TurnoId,
    string? Referencia = null);

public sealed record ConfirmarPropuestaIngresosPeticion(string HuellaEsperada);
