namespace Jardin.Inscripciones.Contracts;

public sealed record BuscarResponsablesRespuesta(
    IReadOnlyList<ConsultarResponsableRespuesta> Resultados,
    int Pagina,
    int TamanoPagina,
    bool HayMas);
