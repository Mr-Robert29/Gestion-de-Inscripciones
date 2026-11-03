using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioPersonasInscripciones : IRepositorioPersonasInscripciones
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioPersonasInscripciones(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioPersonasInscripciones.ObtenerAspiranteAsync: también filtra los responsables visibles.
    public Task<Aspirante?> ObtenerAspiranteAsync(
        Guid institucionId,
        Guid aspiranteId,
        CancellationToken tokenCancelacion = default)
        => _contexto.Aspirantes.AsNoTracking().Where(a => a.Id == aspiranteId && _contexto.AspirantesInstituciones
                .Any(r => r.InstitucionId == institucionId && r.AspiranteId == a.Id))
            .Include(a => a.Vinculos.Where(v => _contexto.ResponsablesInstituciones
                .Any(r => r.InstitucionId == institucionId && r.ResponsableId == v.ResponsableId)))
            .ThenInclude(v => v.Responsable).SingleOrDefaultAsync(tokenCancelacion);

    // Implementa IRepositorioPersonasInscripciones.ObtenerResponsableAsync.
    public Task<Responsable?> ObtenerResponsableAsync(
        Guid institucionId,
        Guid responsableId,
        CancellationToken tokenCancelacion = default)
        => _contexto.Responsables.SingleOrDefaultAsync(r => r.Id == responsableId && _contexto.ResponsablesInstituciones
            .Any(registro => registro.InstitucionId == institucionId && registro.ResponsableId == r.Id), tokenCancelacion);

    // Implementa IRepositorioPersonasInscripciones.BuscarResponsablesAsync.
    public async Task<IReadOnlyList<Responsable>> BuscarResponsablesAsync(
        Guid institucionId,
        string? documento,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default)
    {
        var consulta = _contexto.Responsables.AsNoTracking().Where(r => _contexto.ResponsablesInstituciones
            .Any(registro => registro.InstitucionId == institucionId && registro.ResponsableId == r.Id));
        if (documento is not null) consulta = consulta.Where(r => r.Documento == documento);
        return await consulta.OrderBy(r => r.Apellido).ThenBy(r => r.Nombre).ThenBy(r => r.Id)
            .Skip(desplazamiento).Take(cantidad).ToArrayAsync(tokenCancelacion);
    }

    // Implementa IRepositorioPersonasInscripciones.AgregarAspiranteAsync.
    public Task AgregarAspiranteAsync(
        Aspirante aspirante,
        RegistroAspiranteInstitucion registro,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Aspirantes.Add(aspirante);
        _contexto.AspirantesInstituciones.Add(registro);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    // Implementa IRepositorioPersonasInscripciones.AgregarResponsableAsync.
    public Task AgregarResponsableAsync(
        Responsable responsable,
        RegistroResponsableInstitucion registro,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Responsables.Add(responsable);
        _contexto.ResponsablesInstituciones.Add(registro);
        return GuardarAsync(cambio, tokenCancelacion);
    }

    // Implementa IRepositorioPersonasInscripciones.AgregarVinculoAsync.
    public async Task AgregarVinculoAsync(
        VinculoResponsable vinculo,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        _contexto.Vinculos.Add(vinculo);
        try { await GuardarAsync(cambio, tokenCancelacion); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_vinculos_responsables" })
        {
            throw new ConflictoVinculoException();
        }
    }

    // Implementa IRepositorioPersonasInscripciones.ConsultarHistorialAsync.
    public async Task<IReadOnlyList<CambioInscripciones>> ConsultarHistorialAsync(Guid institucionId, CancellationToken tokenCancelacion = default)
    {
        var cambios = await _contexto.Cambios.AsNoTracking().Where(c => c.InstitucionId == institucionId).ToArrayAsync(tokenCancelacion);
        return cambios.OrderBy(c => c.Fecha).ThenBy(c => c.Id).ToArray();
    }

    private Task GuardarAsync(CambioInscripciones cambio, CancellationToken tokenCancelacion)
    {
        _contexto.Cambios.Add(cambio);
        return _contexto.SaveChangesAsync(tokenCancelacion);
    }
}
