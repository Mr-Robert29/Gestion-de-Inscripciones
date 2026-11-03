namespace Jardin.Inscripciones.Contracts;

// Null no se interpreta como cero. Las revisiones y el último registro previenen cargas desde una pantalla antigua.
public sealed record RegistrarDistanciaManualPeticion(
    decimal? DistanciaMetros,
    string Referencia,
    int RevisionDomicilio,
    int RevisionUbicacionJardin,
    int UltimoNumeroRegistro);
