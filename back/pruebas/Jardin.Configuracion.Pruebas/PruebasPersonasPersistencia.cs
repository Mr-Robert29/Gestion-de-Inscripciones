using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPersonasPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoInscripciones> opciones,
        Guid institucionId,
        Action<bool, string> comprobar)
    {
        var aspirante = new Aspirante(
            "Prueba persistencia",
            "Ficticio",
            null,
            new DateOnly(
                2022,
                1,
                1));
        var responsable = new Responsable(
            "Responsable",
            "Ficticio",
            null,
            "12345");
        CambioInscripciones Cambio(Guid entidadId) => new(
            institucionId,
            Guid.NewGuid(),
            entidadId,
            "Prueba.Persistencia",
            "{}");
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var repositorio = new RepositorioPersonasInscripciones(contexto);
            await repositorio.AgregarAspiranteAsync(
                aspirante,
                new RegistroAspiranteInstitucion(institucionId, aspirante.Id),
                Cambio(aspirante.Id));
            await repositorio.AgregarResponsableAsync(
                responsable,
                new RegistroResponsableInstitucion(institucionId, responsable.Id),
                Cambio(responsable.Id));
        }
        await using var primero = new ContextoInscripciones(opciones);
        await using var segundo = new ContextoInscripciones(opciones);
        var repositorioA = new RepositorioPersonasInscripciones(primero);
        var repositorioB = new RepositorioPersonasInscripciones(segundo);
        var a = (await repositorioA.ObtenerAspiranteAsync(institucionId, aspirante.Id))!;
        var b = (await repositorioB.ObtenerAspiranteAsync(institucionId, aspirante.Id))!;
        var adultoA = (await repositorioA.ObtenerResponsableAsync(institucionId, responsable.Id))!;
        var adultoB = (await repositorioB.ObtenerResponsableAsync(institucionId, responsable.Id))!;
        var vinculoA = a.VincularResponsable(
            adultoA,
            "Tutor",
            true,
            true);
        var vinculoB = b.VincularResponsable(
            adultoB,
            "Tutor",
            true,
            true);
        await repositorioA.AgregarVinculoAsync(vinculoA, Cambio(aspirante.Id));
        try
        {
            await repositorioB.AgregarVinculoAsync(vinculoB, Cambio(aspirante.Id));
            throw new Exception("La base permitió duplicar un vínculo concurrente.");
        }
        catch (ConflictoVinculoException)
        {
            comprobar(true, "PostgreSQL protege dos vinculaciones simultáneas de la misma pareja.");
        }
        catch (DbUpdateException) when (!segundo.Database.IsNpgsql())
        {
            comprobar(true, "La clave compuesta de SQLite impide duplicar el vínculo.");
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(await contexto.Vinculos.CountAsync(v => v.AspiranteId == aspirante.Id) == 1 &&
                await contexto.Cambios.CountAsync(c => c.EntidadId == aspirante.Id) == 2,
                "El vínculo rechazado no agrega otra relación ni una entrada de auditoría.");
        }
        var fallido = new Aspirante(
            "No debe persistir",
            "Prueba ficticia",
            null,
            new DateOnly(
                2022,
                2,
                2));
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var historialInvalido = Cambio(fallido.Id);
            contexto.Cambios.Add(historialInvalido);
            // Simula un fallo real de NOT NULL al guardar la auditoría, después de preparar el alta.
            contexto.Entry(historialInvalido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await new RepositorioPersonasInscripciones(contexto).AgregarAspiranteAsync(
                    fallido,
                    new RegistroAspiranteInstitucion(institucionId, fallido.Id),
                    historialInvalido);
                throw new Exception("Se permitió registrar una persona con historial inválido.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "Un fallo al guardar el historial rechaza el alta completa.");
            }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(!await contexto.Aspirantes.AnyAsync(a => a.Id == fallido.Id) &&
                !await contexto.AspirantesInstituciones.AnyAsync(r => r.AspiranteId == fallido.Id) &&
                !await contexto.Cambios.AnyAsync(c => c.EntidadId == fallido.Id),
                "La transacción revierte persona, registro institucional e historial.");
        }
    }
}
