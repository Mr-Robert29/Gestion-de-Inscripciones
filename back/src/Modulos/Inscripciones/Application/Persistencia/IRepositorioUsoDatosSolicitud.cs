using Jardin.Inscripciones.Domain.Entidades;

namespace Jardin.Inscripciones.Application.Persistencia;

public interface IRepositorioUsoDatosSolicitud
{
    Task RegistrarAsync(UsoDatosSolicitud uso, CancellationToken tokenCancelacion = default);
    Task<IReadOnlyList<UsoDatosSolicitud>> ListarAsync(Guid solicitudId, CancellationToken tokenCancelacion = default);
}
