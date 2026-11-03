using Jardin.Inscripciones.Domain.ObjetosDeValor;

namespace Jardin.Inscripciones.Application.Geografia;

// La API adapta Configuración mediante Contracts, sin leer sus tablas desde Inscripciones.
public interface IUbicacionJardin
{
    Task<UbicacionJardin> ConsultarAsync(Guid institucionId, CancellationToken tokenCancelacion = default);
}

public sealed record UbicacionJardin(
    int RevisionUbicacion,
    Domicilio Domicilio,
    Coordenadas? Coordenadas);
