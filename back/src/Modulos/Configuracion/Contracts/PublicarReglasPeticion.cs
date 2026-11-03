namespace Jardin.Configuracion.Contracts;

// Entrada: la fecha es explícita; el perfil conserva las reglas aprobadas del prototipo.
public sealed record PublicarReglasPeticion(
    DateOnly FechaCorte,
    string Referencia,
    string Motivo);
