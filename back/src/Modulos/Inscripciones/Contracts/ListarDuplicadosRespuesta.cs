namespace Jardin.Inscripciones.Contracts;

public sealed record ListarDuplicadosRespuesta(
    IReadOnlyList<ConsultarDuplicadoRespuesta> Resultados,
    int Pagina,
    int TamanoPagina,
    bool HayMas);
