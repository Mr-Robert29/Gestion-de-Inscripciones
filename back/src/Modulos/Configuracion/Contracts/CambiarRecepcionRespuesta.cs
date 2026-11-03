namespace Jardin.Configuracion.Contracts;

public sealed record CambiarRecepcionRespuesta(
    Guid Id,
    string Estado,
    int Revision);
