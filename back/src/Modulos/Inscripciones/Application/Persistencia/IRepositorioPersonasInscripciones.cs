using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioPersonasInscripciones
{
    Task<Aspirante?> ObtenerAspiranteAsync(
        Guid institucionId,
        Guid aspiranteId,
        CancellationToken tokenCancelacion = default);
    Task<Responsable?> ObtenerResponsableAsync(
        Guid institucionId,
        Guid responsableId,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<Responsable>> BuscarResponsablesAsync(
        Guid institucionId,
        string? documento,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default);
    Task AgregarAspiranteAsync(
        Aspirante aspirante,
        RegistroAspiranteInstitucion registro,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task AgregarResponsableAsync(
        Responsable responsable,
        RegistroResponsableInstitucion registro,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task AgregarVinculoAsync(
        VinculoResponsable vinculo,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<CambioInscripciones>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
}
