namespace Jardin.Inscripciones.Contracts;

public sealed record ListarDuplicadosPeticion(
    string? Estado = null,
    int Pagina = 1,
    int TamanoPagina = 20);
