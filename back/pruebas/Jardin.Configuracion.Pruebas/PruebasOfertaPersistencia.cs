using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Domain.Entidades;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasOfertaPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoConfiguracion> opciones,
        Guid institucionId,
        Action<bool, string> comprobar)
    {
        var aula = new Aula(institucionId, "Aula concurrencia");
        var turno = new Turno(institucionId, "Turno prueba");
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
                15));
        var oferta = new OfertaAulaTurno(
            periodo.Id,
            aula.Id,
            turno.Id,
            20,
            true,
            "Acta de prueba",
            "Relevamiento ficticio",
            [new OcupacionInicial(3, 2)]);
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            contexto.Aulas.Add(aula);
            contexto.Turnos.Add(turno);
            contexto.Periodos.Add(periodo);
            contexto.Ofertas.Add(oferta);
            await contexto.SaveChangesAsync();
        }
        CambioInstitucion Cambio(Guid id) => new(
            id,
            Guid.NewGuid(),
            "Oferta.PruebaPersistencia",
            "Prueba aislada",
            "{}",
            "{}");
        await using var primero = new ContextoConfiguracion(opciones);
        await using var segundo = new ContextoConfiguracion(opciones);
        var a = (await new RepositorioOferta(primero).ObtenerOfertaAsync(oferta.Id))!;
        var b = (await new RepositorioOferta(segundo).ObtenerOfertaAsync(oferta.Id))!;
        a.Actualizar(
            18,
            true,
            "Primera edición",
            "Relevamiento actualizado",
            [new OcupacionInicial(3, 4)]);
        b.Actualizar(
            16,
            true,
            "Segunda edición",
            "Relevamiento concurrente",
            [new OcupacionInicial(3, 6)]);
        await new RepositorioOferta(primero).ActualizarOfertaAsync(a, Cambio(institucionId));
        try
        {
            await new RepositorioOferta(segundo).ActualizarOfertaAsync(b, Cambio(institucionId));
            throw new Exception("Se permitió sobrescribir una oferta concurrente.");
        }
        catch (ConflictoInstitucionException)
        {
            comprobar(true, "La revisión de oferta protege también dos contextos que leyeron simultáneamente.");
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var leida = (await new RepositorioOferta(contexto).ObtenerOfertaAsync(oferta.Id))!;
            comprobar(leida.Capacidad == 18 && leida.Ocupaciones.Single().Cantidad == 4 &&
                await contexto.Cambios.CountAsync(c => c.InstitucionId == institucionId && c.Accion == "Oferta.PruebaPersistencia") == 1,
                "El conflicto revierte ocupaciones e historial junto con la oferta.");
            leida.Actualizar(
                12,
                true,
                "Cambio que debe revertirse",
                "Prueba ficticia",
                []);
            try
            {
                await new RepositorioOferta(contexto).ActualizarOfertaAsync(leida, Cambio(Guid.NewGuid()));
                throw new Exception("Se permitió un historial con institución inexistente.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "Un historial inválido rechaza también la edición de oferta.");
            }
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var leida = (await new RepositorioOferta(contexto).ObtenerOfertaAsync(oferta.Id))!;
            comprobar(leida.Capacidad == 18 && leida.Ocupaciones.Single().Cantidad == 4,
                "El rollback conserva capacidad y ocupaciones anteriores.");
            var duplicada = new OfertaAulaTurno(
                periodo.Id,
                aula.Id,
                turno.Id,
                20,
                true,
                "Duplicado simulado",
                "Prueba ficticia",
                []);
            try
            {
                await new RepositorioOferta(contexto).AgregarOfertaAsync(duplicada, Cambio(institucionId));
                throw new Exception("La base permitió duplicar la combinación.");
            }
            catch (InvalidOperationException error) when (error.InnerException is DbUpdateException)
            {
                comprobar(true, "PostgreSQL impide duplicados aunque el alta evite la comprobación previa de Application.");
            }
            catch (DbUpdateException) when (!contexto.Database.IsNpgsql())
            {
                comprobar(true, "El índice único impide duplicados en SQLite.");
            }
        }
    }
}
