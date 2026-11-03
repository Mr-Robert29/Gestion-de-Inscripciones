namespace Jardin.Configuracion.Contracts;

// Entrada para preparar un período de la institución indicada en la ruta.
public sealed record CrearPeriodoPeticion(
    int Ciclo,
    DateOnly Inicio,
    DateOnly Fin);
