using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioDistancias
{
    Task GuardarAsync(
        MedicionDistancia medicion,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<MedicionDistancia>> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
}
