namespace Jardin.Secciones.Contracts;

public interface IModuloComposicionSecciones
{
    Task<CambiarComposicionSeccionRespuesta> CambiarAsync(
        Guid periodoId,
        Guid seccionId,
        CambiarComposicionSeccionPeticion peticion,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<DatosCambioComposicion>> ConsultarHistorialAsync(
        Guid periodoId,
        Guid seccionId,
        CancellationToken tokenCancelacion = default);
}

public sealed record CambiarComposicionSeccionRespuesta(
    Guid SeccionId,
    int Revision,
    IReadOnlyList<int> EdadesAdmitidas,
    IReadOnlyList<Guid> PropuestasParaRevision);

public sealed record DatosCambioComposicion(
    Guid Id,
    Guid SeccionId,
    string EdadesAnteriores,
    string EdadesNuevas,
    int RevisionAnterior,
    int RevisionNueva,
    Guid UsuarioId,
    DateTimeOffset Fecha,
    string Motivo);
