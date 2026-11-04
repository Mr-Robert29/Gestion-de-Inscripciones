namespace Jardin.Vacantes.Contracts;

// Los filtros no crean listas nuevas ni modifican posiciones.
public sealed record ConsultarListaEsperaPeticion(
    int? Sala = null,
    Guid? TurnoId = null,
    string? Estado = null,
    int Pagina = 1,
    int TamanoPagina = 20);
