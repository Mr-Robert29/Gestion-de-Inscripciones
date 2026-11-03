namespace Jardin.Vacantes.Contracts;

public sealed record RegistrarSorteoPeticion(
    int Numero,
    DateOnly Fecha,
    string Referencia,
    int Sala);
