namespace Jardin.Inscripciones.Contracts;

public sealed record ConsultarAspiranteRespuesta(
    Guid Id,
    string Nombre,
    string Apellido,
    DateOnly FechaNacimiento,
    string? Dni,
    string? TipoDocumento,
    string? Sexo,
    IReadOnlyList<DatosVinculoResponsable> Responsables);
