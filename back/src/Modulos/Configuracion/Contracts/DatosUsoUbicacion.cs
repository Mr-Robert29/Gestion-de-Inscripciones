namespace Jardin.Configuracion.Contracts;

// Referencia a un resultado de otro módulo; no copia la medición ni la propuesta.
public sealed record DatosUsoUbicacion(
    Guid ReferenciaId,
    string Tipo,
    int RevisionUbicacion);
