namespace Jardin.Configuracion.Contracts;

public sealed record AsignarConfiguracionRespuesta(
    Guid PeriodoId,
    DatosVersionesConfiguracion Versiones,
    int Revision);
