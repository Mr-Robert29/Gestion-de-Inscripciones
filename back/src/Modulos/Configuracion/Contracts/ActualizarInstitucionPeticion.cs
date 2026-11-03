namespace Jardin.Configuracion.Contracts;

// Entrada completa de actualización; RevisionDatos evita sobrescribir cambios ajenos.
public sealed record ActualizarInstitucionPeticion(
    string Nombre,
    DatosDomicilio Domicilio,
    string TelefonoContacto,
    int RevisionDatos,
    string Motivo,
    string? Codigo = null,
    string? CorreoElectronicoContacto = null,
    DatosCoordenadas? Coordenadas = null);
