namespace Jardin.Configuracion.Contracts;

// Cada referencia afectada conserva su revisión original para explicar el cambio.
public sealed record ConsultarImpactoUbicacionRespuesta(
    Guid InstitucionId,
    int RevisionActual,
    bool UbicacionPendiente,
    IReadOnlyList<DatosUsoUbicacion> ReferenciasAfectadas);
