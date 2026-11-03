namespace Jardin.Inscripciones.Contracts;

public interface IModuloParticipantesSorteo
{
    Task<IReadOnlyList<DatosParticipanteSorteo>> ListarAsync(
        Guid institucionId,
        Guid periodoId,
        int sala,
        CancellationToken cancellationToken = default);
}
