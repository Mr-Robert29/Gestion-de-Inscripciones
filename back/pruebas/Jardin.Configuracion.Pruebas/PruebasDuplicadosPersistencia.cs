using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.ObjetosDeValor;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasDuplicadosPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoInscripciones> opciones,
        Guid institucionId,
        Guid periodoId,
        Guid solicitudId,
        Guid alertaId,
        Action<bool, string> comprobar)
    {
        CambioInscripciones Cambio(Guid entidad) => new(
            institucionId,
            Guid.NewGuid(),
            entidad,
            "Duplicado.PruebaPersistencia",
            "{}");
        Guid nuevaId;
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var anterior = await contexto.Solicitudes.Include(s => s.TurnosAlternativos).SingleAsync(s => s.Id == solicitudId);
            var aspirante = await contexto.Aspirantes.SingleAsync(a => a.Id == anterior.AspiranteId);
            var nueva = new SolicitudInscripcion(
                institucionId,
                periodoId,
                aspirante,
                new Domicilio(
                    anterior.DomicilioDeclarado.Calle,
                    anterior.DomicilioDeclarado.Numero,
                    anterior.DomicilioDeclarado.Localidad,
                    anterior.DomicilioDeclarado.Provincia),
                anterior.CalculoEdad.FechaCorteUtilizada,
                anterior.TurnoPreferidoId,
                [],
                anterior.VersionReglasId,
                anterior.VersionOfertaId,
                anterior.RevisionPeriodoUtilizada,
                false,
                Guid.NewGuid());
            nuevaId = nueva.Id;
            var invalido = Cambio(nueva.Id);
            contexto.Cambios.Add(invalido);
            contexto.Entry(invalido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await new RepositorioSolicitudes(contexto).AgregarAsync(nueva, invalido);
                throw new Exception("Se permitió guardar una solicitud con historial inválido.");
            }
            catch (DbUpdateException)
            {
                comprobar(contexto.ChangeTracker.Entries<PosibleDuplicado>().Any(e => e.Entity.SolicitudAId == nuevaId || e.Entity.SolicitudBId == nuevaId),
                    "Las alertas se preparan junto con el registro de la solicitud.");
            }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(!await contexto.Solicitudes.AnyAsync(s => s.Id == nuevaId) &&
                !await contexto.PosiblesDuplicados.AnyAsync(a => a.SolicitudAId == nuevaId || a.SolicitudBId == nuevaId) &&
                !await contexto.Cambios.AnyAsync(c => c.EntidadId == nuevaId),
                "Si falla el historial, se revierten conjuntamente solicitud, alertas y cambios.");
        }
        await using var primero = new ContextoInscripciones(opciones);
        await using var segundo = new ContextoInscripciones(opciones);
        var repositorioA = new RepositorioDuplicadosSolicitud(primero);
        var repositorioB = new RepositorioDuplicadosSolicitud(segundo);
        var a = (await repositorioA.ObtenerAsync(institucionId, alertaId))!;
        var b = (await repositorioB.ObtenerAsync(institucionId, alertaId))!;
        var revision = a.Revision;
        a.Resolver(
            EstadoPosibleDuplicado.PersonasDiferentes,
            Guid.NewGuid(),
            "Revisión concurrente A");
        b.Resolver(
            EstadoPosibleDuplicado.DuplicacionConfirmada,
            Guid.NewGuid(),
            "Revisión concurrente B");
        await repositorioA.GuardarResolucionAsync(a, Cambio(alertaId));
        try
        {
            await repositorioB.GuardarResolucionAsync(b, Cambio(alertaId));
            throw new Exception("Se sobrescribió una revisión concurrente.");
        }
        catch (ConflictoDuplicadoException)
        {
            comprobar(true, "Dos revisiones simultáneas no se sobrescriben silenciosamente.");
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var repositorio = new RepositorioDuplicadosSolicitud(contexto);
            var actual = (await repositorio.ObtenerAsync(institucionId, alertaId))!;
            comprobar(actual.Estado == EstadoPosibleDuplicado.PersonasDiferentes && actual.Revision == revision + 1 &&
                await contexto.Cambios.CountAsync(c => c.EntidadId == alertaId && c.Accion == "Duplicado.PruebaPersistencia") == 1,
                "El conflicto conserva únicamente la revisión ganadora y su historial.");
            actual.Resolver(
                EstadoPosibleDuplicado.DuplicacionConfirmada,
                Guid.NewGuid(),
                "Cambio que debe revertirse");
            var invalido = Cambio(alertaId);
            contexto.Cambios.Add(invalido);
            contexto.Entry(invalido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await repositorio.GuardarResolucionAsync(actual, invalido);
                throw new Exception("Se guardó una resolución sin historial válido.");
            }
            catch (DbUpdateException) { comprobar(true, "Una resolución con historial inválido se rechaza completa."); }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var repositorio = new RepositorioDuplicadosSolicitud(contexto);
            var actual = (await repositorio.ObtenerAsync(institucionId, alertaId))!;
            comprobar(actual.Revision == revision + 1 && actual.Estado == EstadoPosibleDuplicado.PersonasDiferentes,
                "El fallo de historial no cambia la decisión guardada.");
            var pareja = await repositorio.LeerParejaActualAsync(actual);
            contexto.PosiblesDuplicados.Add(new PosibleDuplicado(
                pareja[1],
                pareja[0],
                Guid.NewGuid()));
            try
            {
                await contexto.SaveChangesAsync();
                throw new Exception("La base aceptó una segunda alerta para la misma pareja.");
            }
            catch (DbUpdateException) { comprobar(true, "La base impide repetir la pareja A-B incluso si se registra B-A."); }
        }
    }
}


