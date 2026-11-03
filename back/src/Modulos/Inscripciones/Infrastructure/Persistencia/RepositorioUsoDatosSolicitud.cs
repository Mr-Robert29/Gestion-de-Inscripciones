using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioUsoDatosSolicitud : IRepositorioUsoDatosSolicitud
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioUsoDatosSolicitud(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioUsoDatosSolicitud.RegistrarAsync: repetir la misma referencia no duplica el índice.
    public async Task RegistrarAsync(UsoDatosSolicitud uso, CancellationToken tokenCancelacion = default)
    {
        if (await _contexto.UsosDatosSolicitud.AnyAsync(u => u.SolicitudId == uso.SolicitudId && u.ReferenciaId == uso.ReferenciaId &&
            u.Tipo == uso.Tipo && u.RevisionDatos == uso.RevisionDatos && u.RevisionDomicilio == uso.RevisionDomicilio, tokenCancelacion)) return;
        _contexto.UsosDatosSolicitud.Add(uso);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_UsoSolicitud_Referencia_Revision" })
        {
            _contexto.Entry(uso).State = EntityState.Detached;
        }
    }

    // Implementa IRepositorioUsoDatosSolicitud.ListarAsync.
    public async Task<IReadOnlyList<UsoDatosSolicitud>> ListarAsync(Guid solicitudId, CancellationToken tokenCancelacion = default)
        => await _contexto.UsosDatosSolicitud.AsNoTracking().Where(u => u.SolicitudId == solicitudId).OrderBy(u => u.Id).ToArrayAsync(tokenCancelacion);
}
