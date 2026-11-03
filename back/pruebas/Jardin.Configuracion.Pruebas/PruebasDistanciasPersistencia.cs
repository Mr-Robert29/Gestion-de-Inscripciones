using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasDistanciasPersistencia
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
            "Distancia.PruebaPersistencia",
            "{}");
        await using var primero = new ContextoInscripciones(opciones);
        await using var segundo = new ContextoInscripciones(opciones);
        var solicitud = await primero.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId);
        var anterior = await primero.MedicionesDistancia.AsNoTracking().Where(m => m.SolicitudId == solicitudId).OrderByDescending(m => m.NumeroRegistro).FirstAsync();
        MedicionDistancia Nueva(decimal metros) => new(solicitud, new RegistroDistanciaManual(
            metros,
            anterior.RevisionUbicacionJardin,
            anterior.NumeroRegistro + 1,
            Guid.NewGuid(),
            "Prueba de persistencia",
            anterior.Entradas));
        var a = Nueva(800.123456789m);
        var b = Nueva(700m);
        await new RepositorioDistancias(primero).GuardarAsync(a, Cambio(a.Id));
        try
        {
            await new RepositorioDistancias(segundo).GuardarAsync(b, Cambio(b.Id));
            throw new Exception("Se aceptaron dos registros con el mismo número esperado.");
        }
        catch (ConflictoRevisionSolicitudException)
        { comprobar(true, "Una carga desde una revisión anterior no sustituye otra distancia registrada."); }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(await contexto.MedicionesDistancia.CountAsync(m => m.SolicitudId == solicitudId) == anterior.NumeroRegistro + 1 &&
                !await contexto.Cambios.AnyAsync(c => c.EntidadId == b.Id),
                "La carga rechazada no deja distancia ni historial parciales.");
            var registro = await contexto.MedicionesDistancia.SingleAsync(m => m.Id == a.Id);
            comprobar(registro.DistanciaMetros == 800.123456789m && registro.Entradas == anterior.Entradas,
                "La persistencia conserva los decimales y la copia de las entradas utilizadas.");
            contexto.Entry(registro).Property(m => m.DistanciaMetros).CurrentValue = 1;
            try
            {
                await contexto.SaveChangesAsync();
                throw new Exception("Se pudo sobrescribir el historial.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("El historial de distancias"))
            { comprobar(true, "El contexto impide sobrescribir una medición histórica."); }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            contexto.MedicionesDistancia.Remove((await contexto.MedicionesDistancia.FindAsync(a.Id))!);
            try
            {
                contexto.SaveChanges();
                throw new Exception("Se pudo borrar el historial.");
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("El historial de distancias"))
            { comprobar(true, "El guardado síncrono también impide borrar una medición histórica."); }
        }
        var cambioFallido = Cambio(Guid.NewGuid());
        Guid fallidaId;
        int revisionAntes;
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            solicitud = await contexto.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId);
            revisionAntes = solicitud.RevisionDatos;
            anterior = await contexto.MedicionesDistancia.AsNoTracking().Where(m => m.SolicitudId == solicitudId).OrderByDescending(m => m.NumeroRegistro).FirstAsync();
            var fallida = Nueva(10);
            fallidaId = fallida.Id;
            contexto.Cambios.Add(cambioFallido);
            contexto.Entry(cambioFallido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await new RepositorioDistancias(contexto).GuardarAsync(fallida, cambioFallido);
                throw new Exception("Se guardó una distancia sin historial válido.");
            }
            catch (DbUpdateException) { comprobar(true, "Un fallo del historial rechaza el registro manual completo."); }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(!await contexto.MedicionesDistancia.AnyAsync(m => m.Id == fallidaId) &&
                !await contexto.UsosDatosSolicitud.AnyAsync(u => u.ReferenciaId == fallidaId) &&
                !await contexto.Cambios.AnyAsync(c => c.Id == cambioFallido.Id) &&
                (await contexto.Solicitudes.AsNoTracking().SingleAsync(s => s.Id == solicitudId)).RevisionDatos == revisionAntes,
                "El rollback conserva la revisión de solicitud y no deja medición, referencia ni historial parciales.");
        }
    }
}
