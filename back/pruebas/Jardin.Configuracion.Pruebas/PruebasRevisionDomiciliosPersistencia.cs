using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasRevisionDomiciliosPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoInscripciones> opciones,
        Guid institucionId,
        Guid solicitudId,
        Action<bool, string> comprobar)
    {
        CambioInscripciones Cambio(Guid id) => new(
            institucionId,
            Guid.NewGuid(),
            id,
            "Domicilio.PruebaPersistencia",
            "{}");
        await using var primero = new ContextoInscripciones(opciones);
        await using var segundo = new ContextoInscripciones(opciones);
        var solicitud = await primero.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId);
        var ultimo = await primero.RevisionesDomicilio.AsNoTracking().Where(r => r.SolicitudId == solicitudId).MaxAsync(r => r.NumeroRegistro);
        RevisionManualDomicilio Nueva(EstadoRevisionDomicilio estado) => new(solicitud, new DatosDecisionRevisionDomicilio(
            estado,
            ultimo + 1,
            Guid.NewGuid(),
            "Prueba de persistencia",
            "Referencia ficticia"));
        var a = Nueva(EstadoRevisionDomicilio.Observado);
        var b = Nueva(EstadoRevisionDomicilio.RevisadoManualmente);
        await new RepositorioRevisionDomicilios(primero).GuardarAsync(a, Cambio(a.Id));
        try
        {
            await new RepositorioRevisionDomicilios(segundo).GuardarAsync(b, Cambio(b.Id));
            throw new Exception("Se sobrescribió una revisión de domicilio más reciente.");
        }
        catch (ConflictoRevisionSolicitudException)
        { comprobar(true, "Una decisión desde una revisión anterior se rechaza sin sobrescribir otra."); }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(await contexto.RevisionesDomicilio.CountAsync(r => r.SolicitudId == solicitudId) == ultimo + 1 &&
                !await contexto.Cambios.AnyAsync(c => c.EntidadId == b.Id),
                "El conflicto no deja una segunda decisión ni historial parcial.");
            var leida = await contexto.RevisionesDomicilio.SingleAsync(r => r.Id == a.Id);
            comprobar(leida.Domicilio == a.Domicilio && leida.Estado == EstadoRevisionDomicilio.Observado,
                "La lectura reconstruye el domicilio histórico completo y su decisión.");
            contexto.Entry(leida.Domicilio).Property(d => d.Calle).CurrentValue = "Cambio histórico prohibido";
            try
            {
                await contexto.SaveChangesAsync();
                throw new Exception("Se modificó la dirección del historial.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("El historial de revisión de domicilios"))
            { comprobar(true, "El contexto protege también la copia del domicilio dentro del historial."); }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            contexto.RevisionesDomicilio.Remove((await contexto.RevisionesDomicilio.FindAsync(a.Id))!);
            try
            {
                contexto.SaveChanges();
                throw new Exception("Se borró una revisión histórica.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("El historial de revisión de domicilios"))
            { comprobar(true, "El guardado síncrono impide borrar decisiones históricas."); }
        }
        var invalido = Cambio(Guid.NewGuid());
        Guid fallidaId;
        int revisionAntes;
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            solicitud = await contexto.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId);
            revisionAntes = solicitud.RevisionDatos;
            ultimo = await contexto.RevisionesDomicilio.Where(r => r.SolicitudId == solicitudId).MaxAsync(r => r.NumeroRegistro);
            var fallida = Nueva(EstadoRevisionDomicilio.PendienteRevision);
            fallidaId = fallida.Id;
            contexto.Cambios.Add(invalido);
            contexto.Entry(invalido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await new RepositorioRevisionDomicilios(contexto).GuardarAsync(fallida, invalido);
                throw new Exception("Se guardó una decisión sin auditoría válida.");
            }
            catch (DbUpdateException) { comprobar(true, "Una auditoría inválida rechaza la decisión completa."); }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(!await contexto.RevisionesDomicilio.AnyAsync(r => r.Id == fallidaId) &&
                !await contexto.Cambios.AnyAsync(c => c.Id == invalido.Id) &&
                (await contexto.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).RevisionDatos == revisionAntes,
                "El rollback conserva la revisión de solicitud y no deja decisión ni historial parciales.");
        }
    }
}
