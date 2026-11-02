namespace Jardin.Configuracion.Contracts;

// Entrada del registro: reúne los datos que recibe el módulo para crear la institución.
public sealed record RegistrarInstitucionPeticion(
    string Nombre,
    DatosDomicilio Domicilio,
    string TelefonoContacto,
    string? Codigo = null,
    string? CorreoElectronicoContacto = null,
    DatosCoordenadas? Coordenadas = null);
