namespace Jardin.Inscripciones.Contracts;

// Documento es un filtro exacto opcional, nunca una autorización para consultar otra institución.
public sealed record BuscarResponsablesPeticion(
    string? Documento = null,
    int Pagina = 1,
    int TamanoPagina = 20);
