namespace Jardin.Configuracion.Contracts;

// Salida de actualización: permite continuar trabajando con las revisiones que se guardaron.
public sealed record ActualizarInstitucionRespuesta(
    Guid Id,
    int RevisionDatos,
    int RevisionUbicacion);
