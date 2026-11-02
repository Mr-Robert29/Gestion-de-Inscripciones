namespace Jardin.Configuracion.Contracts;

// Salida de consulta: coordenadas null significa ubicación pendiente, nunca cero metros.
public sealed record ConsultarInstitucionRespuesta(
    Guid Id,
    string Nombre,
    DatosDomicilio Domicilio,
    string TelefonoContacto,
    string? Codigo,
    string? CorreoElectronicoContacto,
    DatosCoordenadas? Coordenadas,
    bool Activa,
    int RevisionUbicacion,
    int RevisionDatos);
