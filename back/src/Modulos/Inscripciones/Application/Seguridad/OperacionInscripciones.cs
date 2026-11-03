namespace Jardin.Inscripciones.Application.Seguridad;

// Operaciones propias del módulo; el host las traduce a los permisos actuales de Usuarios.
public enum OperacionInscripciones
{
    Consultar,
    RegistrarPersonasOSolicitudes,
    RegistrarDocumentacion,
    VerificarCondiciones,
    DetectarDuplicados,
    ResolverDuplicados,
    RegistrarDistanciaManual,
    CorregirDuranteRecepcion,
    CorregirDespuesDelCierre
}
