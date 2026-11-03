namespace Jardin.Configuracion.Contracts;

public sealed record AsignarConfiguracionPeticion(
    DatosVersionesConfiguracion Versiones,
    int Revision,
    string Motivo);
