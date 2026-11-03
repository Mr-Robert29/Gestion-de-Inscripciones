namespace Jardin.Configuracion.Contracts;

// Instantáneas históricas conservadas como JSON, sin sobrescribir valores anteriores.
public sealed record DatosCambioInstitucion(
    Guid Id,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string Accion,
    string Motivo,
    string? DatosAnteriores,
    string DatosNuevos);
