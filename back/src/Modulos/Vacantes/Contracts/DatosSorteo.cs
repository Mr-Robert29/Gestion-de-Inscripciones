namespace Jardin.Vacantes.Contracts;

public sealed record DatosSorteo(
    Guid Id,
    Guid PeriodoId,
    int Sala,
    string Numero,
    DateOnly Fecha,
    string Referencia,
    int Version,
    Guid UsuarioId,
    DateTimeOffset FechaRegistro);
