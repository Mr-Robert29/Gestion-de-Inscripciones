namespace Jardin.Inscripciones.Contracts;

public sealed record ListarSolicitudesRespuesta(
    IReadOnlyList<ConsultarSolicitudRespuesta> Resultados,
    int Pagina,
    int TamanoPagina,
    bool HayMas);
