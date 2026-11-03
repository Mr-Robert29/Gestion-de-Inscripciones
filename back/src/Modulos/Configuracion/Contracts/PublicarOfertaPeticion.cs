namespace Jardin.Configuracion.Contracts;

// Publica una copia de toda la oferta actual del período, sin asignarla automáticamente.
public sealed record PublicarOfertaPeticion(string Referencia, string Motivo);
