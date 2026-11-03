using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Application.Persistencia;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

public sealed class RepositorioCorreccionesSolicitudes : IRepositorioCorreccionesSolicitudes
{
    private readonly ContextoInscripciones _contexto;
    public RepositorioCorreccionesSolicitudes(ContextoInscripciones contexto) { _contexto = contexto; }

    // Implementa IRepositorioCorreccionesSolicitudes.ObtenerEditableAsync.
    public Task<SolicitudInscripcion?> ObtenerEditableAsync(
        Guid institucionId,
        Guid solicitudId,
        CancellationToken tokenCancelacion = default)
        => _contexto.Solicitudes.AsSingleQuery().Include(s => s.TurnosAlternativos)
            .SingleOrDefaultAsync(s => s.InstitucionId == institucionId && s.Id == solicitudId, tokenCancelacion);

    // Implementa IRepositorioCorreccionesSolicitudes.BuscarAsync: filtros y paginación se ejecutan en la base.
    public async Task<IReadOnlyList<DatosSolicitudEncontrada>> BuscarAsync(
        Guid institucionId,
        BuscarSolicitudesPeticion filtro,
        int desplazamiento,
        int cantidad,
        CancellationToken tokenCancelacion = default)
    {
        var consulta = from solicitud in _contexto.Solicitudes.AsNoTracking()
                       join aspirante in _contexto.Aspirantes on solicitud.AspiranteId equals aspirante.Id
                       where solicitud.InstitucionId == institucionId
                       select new { Solicitud = solicitud, Aspirante = aspirante };
        if (filtro.SolicitudId.HasValue) consulta = consulta.Where(r => r.Solicitud.Id == filtro.SolicitudId);
        if (filtro.PeriodoId.HasValue) consulta = consulta.Where(r => r.Solicitud.PeriodoId == filtro.PeriodoId);
        if (!string.IsNullOrWhiteSpace(filtro.Nombre))
        {
            var nombre = filtro.Nombre.Trim();
            consulta = consulta.Where(r => TextoBusquedaInscripciones.Mayusculas(r.Aspirante.Nombre).Contains(TextoBusquedaInscripciones.Mayusculas(nombre)));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Apellido))
        {
            var apellido = filtro.Apellido.Trim();
            consulta = consulta.Where(r => TextoBusquedaInscripciones.Mayusculas(r.Aspirante.Apellido).Contains(TextoBusquedaInscripciones.Mayusculas(apellido)));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Dni))
        {
            var dni = filtro.Dni.Trim();
            consulta = consulta.Where(r => r.Aspirante.Dni == dni);
        }
        var resultados = await consulta.OrderBy(r => r.Solicitud.Id).Skip(desplazamiento).Take(cantidad)
            .Select(r => new
            {
                r.Solicitud.Id,
                r.Solicitud.PeriodoId,
                r.Solicitud.AspiranteId,
                r.Aspirante.Nombre,
                r.Aspirante.Apellido,
                r.Aspirante.Dni,
                r.Solicitud.CalculoEdad.Sala,
                r.Solicitud.EstadoRecepcion,
                r.Solicitud.RevisionDatos
            }).ToArrayAsync(tokenCancelacion);
        return resultados.Select(r => new DatosSolicitudEncontrada(
            r.Id,
            r.PeriodoId,
            r.AspiranteId,
            r.Nombre,
            r.Apellido,
            r.Dni,
            r.Sala,
            r.EstadoRecepcion.ToString(),
            r.RevisionDatos)).ToArray();
    }

    // Implementa IRepositorioCorreccionesSolicitudes.GuardarAsync: datos corregidos e historial son atómicos.
    public async Task GuardarAsync(
        SolicitudInscripcion solicitud,
        CambioInscripciones cambio,
        CancellationToken tokenCancelacion = default)
    {
        if (_contexto.Entry(solicitud).State == EntityState.Detached)
            throw new InvalidOperationException("Consultá la solicitud antes de corregirla.");
        _contexto.Cambios.Add(cambio);
        try { await _contexto.SaveChangesAsync(tokenCancelacion); }
        catch (DbUpdateConcurrencyException) { throw new ConflictoRevisionSolicitudException(); }
    }
}
