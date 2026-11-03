namespace Jardin.Inscripciones.Contracts;

public sealed record BuscarSolicitudesRespuesta(
    IReadOnlyList<DatosSolicitudEncontrada> Resultados,
    int Pagina,
    int TamanoPagina,
    bool HayMas);
