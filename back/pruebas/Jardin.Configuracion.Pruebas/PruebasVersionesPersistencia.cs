using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Domain.Entidades;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasVersionesPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoConfiguracion> opciones,
        Guid institucionId,
        Action<bool, string> comprobar)
    {
        var periodo = new PeriodoInscripcion(
            institucionId,
            2027,
            new DateOnly(
                2026,
                11,
                1),
            new DateOnly(
                2026,
                12,
                1));
        VersionConfiguracion Version(TipoVersionConfiguracion tipo) => new(
            periodo.Id,
            tipo,
            "{\"prueba\":true}",
            Guid.NewGuid(),
            "Prueba aislada de persistencia",
            "Referencia ficticia");
        var reglas = Version(TipoVersionConfiguracion.Reglas);
        var oferta = Version(TipoVersionConfiguracion.Oferta);
        var otrasReglas = Version(TipoVersionConfiguracion.Reglas);
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            contexto.Periodos.Add(periodo);
            contexto.VersionesConfiguracion.AddRange(
                reglas,
                oferta,
                otrasReglas);
            await contexto.SaveChangesAsync();
        }
        CambioInstitucion Cambio(Guid id) => new(
            id,
            Guid.NewGuid(),
            "Configuracion.PruebaPersistencia",
            "Prueba aislada",
            "{}",
            "{}");
        await using var primero = new ContextoConfiguracion(opciones);
        await using var segundo = new ContextoConfiguracion(opciones);
        var a = (await new RepositorioPeriodos(primero).ObtenerAsync(periodo.Id))!;
        var b = (await new RepositorioPeriodos(segundo).ObtenerAsync(periodo.Id))!;
        a.AsignarConfiguracion(reglas, oferta);
        b.AsignarConfiguracion(otrasReglas, oferta);
        await new RepositorioPeriodos(primero).ActualizarAsync(a, Cambio(institucionId));
        try
        {
            await new RepositorioPeriodos(segundo).ActualizarAsync(b, Cambio(institucionId));
            throw new Exception("Se permitió reemplazar una asignación concurrente.");
        }
        catch (ConflictoInstitucionException)
        {
            comprobar(true, "Dos asignaciones simultáneas no sobrescriben las versiones del período.");
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var leido = (await new RepositorioPeriodos(contexto).ObtenerAsync(periodo.Id))!;
            comprobar(leido.VersionReglasId == reglas.Id && leido.VersionOfertaId == oferta.Id &&
                await contexto.Cambios.CountAsync(c => c.InstitucionId == institucionId && c.Accion == "Configuracion.PruebaPersistencia") == 1,
                "La asignación ganadora se reconstruye y el conflicto no deja historial adicional.");
            leido.AsignarConfiguracion(otrasReglas, oferta);
            try
            {
                await new RepositorioPeriodos(contexto).ActualizarAsync(leido, Cambio(Guid.NewGuid()));
                throw new Exception("Se permitió asignar versiones con historial inválido.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "El fallo de historial rechaza la asignación completa.");
            }
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            comprobar((await contexto.Periodos.SingleAsync(p => p.Id == periodo.Id)).VersionReglasId == reglas.Id,
                "El rollback conserva las versiones previamente asignadas.");
            var version = (await contexto.VersionesConfiguracion.SingleAsync(v => v.Id == reglas.Id));
            contexto.Entry(version).Property(v => v.Datos).CurrentValue = "{}";
            try
            {
                await contexto.SaveChangesAsync();
                throw new Exception("Se permitió modificar una versión publicada.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Las versiones publicadas"))
            {
                comprobar(true, "La persistencia rechaza modificaciones accidentales de versiones.");
            }
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var version = (await contexto.VersionesConfiguracion.SingleAsync(v => v.Id == otrasReglas.Id));
            contexto.VersionesConfiguracion.Remove(version);
            try
            {
                contexto.SaveChanges();
                throw new Exception("Se permitió eliminar una versión publicada.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Las versiones publicadas"))
            {
                comprobar(true, "El guardado síncrono también protege las versiones contra borrado.");
            }
        }
        var fallida = Version(TipoVersionConfiguracion.Reglas);
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            try
            {
                await new RepositorioVersionesConfiguracion(contexto).AgregarAsync(fallida, Cambio(Guid.NewGuid()));
                throw new Exception("Se publicó una versión sin historial válido.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "Una publicación con historial inválido se rechaza.");
            }
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            comprobar(!await contexto.VersionesConfiguracion.AnyAsync(v => v.Id == fallida.Id) &&
                (await contexto.VersionesConfiguracion.SingleAsync(v => v.Id == reglas.Id)).Datos == reglas.Datos,
                "No quedan publicaciones parciales ni se altera el contenido histórico.");
        }
    }
}
