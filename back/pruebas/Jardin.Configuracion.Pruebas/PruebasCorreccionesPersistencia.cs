using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Domain.ObjetosDeValor;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasCorreccionesPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoInscripciones> opciones,
        Guid institucionId,
        Guid solicitudId,
        Action<bool, string> comprobar)
    {
        CambioInscripciones Cambio() => new(
            institucionId,
            Guid.NewGuid(),
            solicitudId,
            "Correccion.PruebaPersistencia",
            "{}");
        Domicilio Direccion(string calle) => new(
            calle,
            "10",
            "La Plata",
            "Buenos Aires");
        await using var primero = new ContextoInscripciones(opciones);
        await using var segundo = new ContextoInscripciones(opciones);
        var repositorioA = new RepositorioCorreccionesSolicitudes(primero);
        var repositorioB = new RepositorioCorreccionesSolicitudes(segundo);
        var a = (await repositorioA.ObtenerEditableAsync(institucionId, solicitudId))!;
        var b = (await repositorioB.ObtenerEditableAsync(institucionId, solicitudId))!;
        var revisionInicial = a.RevisionDatos;
        var domicilioInicial = a.RevisionDomicilio;
        a.CorregirDatos(
            Direccion("Primera corrección simultánea"),
            a.TurnoPreferidoId,
            a.TurnosAlternativos.Select(t => t.TurnoId).ToArray(),
            a.ReferenciaConsentimiento);
        b.CorregirDatos(
            Direccion("Segunda corrección simultánea"),
            b.TurnoPreferidoId,
            b.TurnosAlternativos.Select(t => t.TurnoId).ToArray(),
            b.ReferenciaConsentimiento);
        await repositorioA.GuardarAsync(a, Cambio());
        try
        {
            await repositorioB.GuardarAsync(b, Cambio());
            throw new Exception("Se permitió sobrescribir una corrección concurrente.");
        }
        catch (ConflictoRevisionSolicitudException)
        {
            comprobar(true, "La base impide sobrescribir correcciones simultáneas de una solicitud.");
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var leida = (await new RepositorioCorreccionesSolicitudes(contexto).ObtenerEditableAsync(institucionId, solicitudId))!;
            comprobar(leida.DomicilioDeclarado.Calle == "Primera corrección simultánea" && leida.RevisionDatos == revisionInicial + 1 &&
                leida.RevisionDomicilio == domicilioInicial + 1 &&
                await contexto.Cambios.CountAsync(c => c.EntidadId == solicitudId && c.Accion == "Correccion.PruebaPersistencia") == 1,
                "Una corrección concurrente rechazada no deja dirección, revisión ni historial parciales.");
        }
        var cambioFallido = Cambio();
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var repositorio = new RepositorioCorreccionesSolicitudes(contexto);
            var leida = (await repositorio.ObtenerEditableAsync(institucionId, solicitudId))!;
            leida.CorregirDatos(
                Direccion("Cambio que debe revertirse"),
                leida.TurnoPreferidoId,
                [],
                "Referencia que no debe guardarse");
            contexto.Cambios.Add(cambioFallido);
            contexto.Entry(cambioFallido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await repositorio.GuardarAsync(leida, cambioFallido);
                throw new Exception("Se guardó una corrección con historial inválido.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "El fallo de historial rechaza la corrección completa.");
            }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var leida = (await new RepositorioCorreccionesSolicitudes(contexto).ObtenerEditableAsync(institucionId, solicitudId))!;
            comprobar(leida.DomicilioDeclarado.Calle == "Primera corrección simultánea" && leida.RevisionDatos == revisionInicial + 1 &&
                leida.RevisionDomicilio == domicilioInicial + 1 && leida.TurnosAlternativos.Count > 0 &&
                !await contexto.Cambios.AnyAsync(c => c.Id == cambioFallido.Id),
                "El rollback conserva dirección, alternativas, revisiones e historial previos.");
        }
    }
}
