using Jardin.Configuracion.Domain.Entidades;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.ObjetosDeValor;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasSolicitudesPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoInscripciones> opciones,
        DbContextOptions<ContextoConfiguracion> configuracion,
        Guid institucionId,
        Guid aspiranteId,
        Action<bool, string> comprobar)
    {
        await using var lectura = new ContextoInscripciones(opciones);
        var aspirante = await lectura.Aspirantes.SingleAsync(a => a.Id == aspiranteId);
        var domicilio = new Domicilio(
            "Calle ficticia",
            "1",
            "La Plata",
            "Buenos Aires");
        var solicitud = new SolicitudInscripcion(
            institucionId,
            Guid.NewGuid(),
            aspirante,
            domicilio,
            new DateOnly(
                2027,
                6,
                30),
            Guid.NewGuid(),
            [Guid.NewGuid()],
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            false,
            Guid.NewGuid());
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var cambio = new CambioInscripciones(
                institucionId,
                Guid.NewGuid(),
                solicitud.Id,
                "Solicitud.PruebaRollback",
                "{}");
            contexto.Cambios.Add(cambio);
            contexto.Entry(cambio).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await new RepositorioSolicitudes(contexto).AgregarAsync(solicitud, cambio);
                throw new Exception("Se guardó una solicitud con auditoría inválida.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "Una auditoría inválida rechaza el registro completo de solicitud.");
            }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(!await contexto.Solicitudes.AnyAsync(s => s.Id == solicitud.Id) &&
                !await contexto.Cambios.AnyAsync(c => c.EntidadId == solicitud.Id),
                "La transacción no deja una solicitud parcial cuando falla el historial.");
        }
        var nacimientos = new[]
        {
            (new DateOnly(
                2024,
                6,
                30), 3),
            (new DateOnly(
                2023,
                6,
                30), 4),
            (new DateOnly(
                2022,
                6,
                30), 5),
            (new DateOnly(
                2022,
                7,
                1), 4),
            (new DateOnly(
                2022,
                6,
                29), 5)
        };
        foreach (var (nacimiento, sala) in nacimientos)
        {
            var persona = new Aspirante(
                "Prueba edad",
                "Ficticio",
                null,
                nacimiento);
            comprobar(new CalculoEdadSolicitud(persona, new DateOnly(
                2027,
                6,
                30)).Sala == sala,
                "El cálculo respeta cumpleaños antes, en y después del corte para salas de tres, cuatro y cinco.");
        }
        await using var comprobacion = new ContextoConfiguracion(configuracion);
        if (!comprobacion.Database.IsNpgsql()) return;
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
        periodo.Abrir();
        comprobacion.Periodos.Add(periodo);
        await comprobacion.SaveChangesAsync();
        var entro = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liberar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var contextoProtegido = new ContextoConfiguracion(configuracion);
        var protegido = new RepositorioPeriodos(contextoProtegido).EjecutarConBloqueoAsync(
            periodo.Id,
            async (actual, token) =>
            {
                entro.SetResult();
                await liberar.Task.WaitAsync(token);
                return actual.Estado;
            });
        try
        {
            await entro.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await using var cierre = new ContextoConfiguracion(configuracion);
            await using var transaccion = await cierre.Database.BeginTransactionAsync();
            await cierre.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'");
            var leido = (await new RepositorioPeriodos(cierre).ObtenerAsync(periodo.Id))!;
            leido.CerrarRecepcion();
            var cambio = new CambioInstitucion(
                institucionId,
                Guid.NewGuid(),
                "Periodo.PruebaCierreConcurrente",
                "Prueba aislada",
                null,
                "{}");
            try
            {
                await new RepositorioPeriodos(cierre).ActualizarAsync(leido, cambio);
                throw new Exception("El cierre modificó el período mientras estaba protegido para registrar.");
            }
            catch (Exception error) when (error.GetBaseException() is PostgresException { SqlState: "55P03" })
            {
                comprobar(true, "PostgreSQL bloquea un cierre concurrente mientras se registra bajo el estado protegido.");
            }
            await transaccion.RollbackAsync();
        }
        finally { liberar.TrySetResult(); }
        comprobar(await protegido.WaitAsync(TimeSpan.FromSeconds(10)) == EstadoPeriodo.RecepcionAbierta,
            "La operación protegida mantiene el estado abierto hasta finalizar.");
        await using (var cierre = new ContextoConfiguracion(configuracion))
        {
            var leido = (await new RepositorioPeriodos(cierre).ObtenerAsync(periodo.Id))!;
            leido.CerrarRecepcion();
            await new RepositorioPeriodos(cierre).ActualizarAsync(leido, new CambioInstitucion(
                institucionId,
                Guid.NewGuid(),
                "Periodo.PruebaCierrePosterior",
                "Finalizó la operación protegida",
                null,
                "{}"));
            comprobar(true, "Después de liberar el período, el cierre puede guardarse normalmente.");
        }
    }
}
