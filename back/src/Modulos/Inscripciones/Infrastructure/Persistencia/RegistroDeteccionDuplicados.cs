using System.Text.Json.Serialization;
using System.Text.Json;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Inscripciones.Infrastructure.Persistencia;

// Se usa antes de SaveChanges para guardar solicitud, alertas y auditoría en una sola transacción.
internal sealed class RegistroDeteccionDuplicados
{
    private static readonly JsonSerializerOptions OpcionesAuditoria = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly ContextoInscripciones _contexto;
    public RegistroDeteccionDuplicados(ContextoInscripciones contexto) { _contexto = contexto; }

    public IQueryable<IdentificacionSolicitud> Identificaciones(
        Guid institucionId,
        Guid periodoId,
        Guid[]? solicitudes = null)
    {
        var consulta = from solicitud in _contexto.Solicitudes.AsNoTracking()
                       join aspirante in _contexto.Aspirantes.AsNoTracking() on solicitud.AspiranteId equals aspirante.Id
                       where solicitud.InstitucionId == institucionId && solicitud.PeriodoId == periodoId
                       select new { Solicitud = solicitud, Aspirante = aspirante };
        if (solicitudes is not null) consulta = consulta.Where(r => solicitudes.Contains(r.Solicitud.Id));
        return consulta.Select(r => new IdentificacionSolicitud(
            r.Solicitud.Id,
            r.Solicitud.InstitucionId,
            r.Solicitud.PeriodoId,
            r.Aspirante.Id,
            r.Aspirante.Nombre,
            r.Aspirante.Apellido,
            r.Aspirante.Dni,
            r.Aspirante.FechaNacimiento));
    }

    public async Task<int> PrepararNuevaSolicitudAsync(
        SolicitudInscripcion solicitud,
        Guid autor,
        CancellationToken tokenCancelacion)
    {
        var aspirante = await _contexto.Aspirantes.AsNoTracking().SingleAsync(a => a.Id == solicitud.AspiranteId, tokenCancelacion);
        var nueva = new IdentificacionSolicitud(
            solicitud.Id,
            solicitud.InstitucionId,
            solicitud.PeriodoId,
            aspirante.Id,
            aspirante.Nombre,
            aspirante.Apellido,
            aspirante.Dni,
            aspirante.FechaNacimiento);
        var existentes = await Identificaciones(solicitud.InstitucionId, solicitud.PeriodoId).ToArrayAsync(tokenCancelacion);
        var cantidad = 0;
        foreach (var existente in existentes)
        {
            if (DetectorPosiblesDuplicados.Evaluar(nueva, existente) == CriterioCoincidenciaSolicitud.Ninguno) continue;
            Agregar(new PosibleDuplicado(
                nueva,
                existente,
                autor));
            cantidad++;
        }
        return cantidad;
    }

    public void Agregar(PosibleDuplicado alerta)
    {
        _contexto.PosiblesDuplicados.Add(alerta);
        _contexto.Cambios.Add(CrearCambio(
            alerta,
            "Duplicado.Deteccion",
            alerta.UsuarioUltimaDeteccionId));
    }

    public static string Serializar(PosibleDuplicado alerta) => JsonSerializer.Serialize(alerta, OpcionesAuditoria);

    public static CambioInscripciones CrearCambio(
        PosibleDuplicado alerta,
        string accion,
        Guid autor,
        string? anteriores = null)
        => new(
            alerta.InstitucionId,
            autor,
            alerta.Id,
            accion,
            JsonSerializer.Serialize(new
            {
                AlertaId = alerta.Id,
                alerta.SolicitudAId,
                alerta.SolicitudBId,
                Anteriores = anteriores is null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(anteriores),
                Nuevos = JsonSerializer.Deserialize<JsonElement>(Serializar(alerta))
            }));
}
