using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioRevisionDomicilios
{
    Task<IReadOnlyList<RevisionManualDomicilio>> ConsultarAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default);
    Task GuardarAsync(
        RevisionManualDomicilio revision,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default);
}
