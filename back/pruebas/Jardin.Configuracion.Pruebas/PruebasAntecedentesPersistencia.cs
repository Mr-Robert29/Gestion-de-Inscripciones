using Jardin.Inscripciones.Application.Excepciones;
using Jardin.Inscripciones.Domain.Entidades;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasAntecedentesPersistencia
{
    public static async Task EjecutarAsync(
        DbContextOptions<ContextoInscripciones> opciones,
        Guid institucionId,
        Guid solicitudId,
        Guid documentoId,
        Action<bool, string> comprobar)
    {
        CambioInscripciones Cambio() => new(
            institucionId,
            Guid.NewGuid(),
            solicitudId,
            "Antecedente.PruebaPersistencia",
            "{}");
        await using var primero = new ContextoInscripciones(opciones);
        await using var segundo = new ContextoInscripciones(opciones);
        var repositorioA = new RepositorioAntecedentesSolicitud(primero);
        var repositorioB = new RepositorioAntecedentesSolicitud(segundo);
        var a = (await repositorioA.ObtenerAsync(institucionId, solicitudId))!;
        var b = (await repositorioB.ObtenerAsync(institucionId, solicitudId))!;
        var revisionInicial = a.RevisionDatos;
        a.Documentos.Single(d => d.Id == documentoId).Revisar(
            EstadoDocumentacionSolicitud.Observada,
            "Primera revisión",
            "Prueba ficticia A",
            Guid.NewGuid());
        b.Documentos.Single(d => d.Id == documentoId).Revisar(
            EstadoDocumentacionSolicitud.Pendiente,
            "Segunda revisión",
            "Prueba ficticia B",
            Guid.NewGuid());
        a.RegistrarRevisionAntecedentes();
        b.RegistrarRevisionAntecedentes();
        await repositorioA.GuardarRevisionAsync(a, Cambio());
        try
        {
            await repositorioB.GuardarRevisionAsync(b, Cambio());
            throw new Exception("Se permitió sobrescribir una revisión concurrente.");
        }
        catch (ConflictoRevisionSolicitudException)
        {
            comprobar(true, "Dos revisiones simultáneas de antecedentes no se sobrescriben.");
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var solicitud = (await new RepositorioAntecedentesSolicitud(contexto).ObtenerAsync(institucionId, solicitudId))!;
            comprobar(solicitud.RevisionDatos == revisionInicial + 1 &&
                solicitud.Documentos.Single(d => d.Id == documentoId).Referencia == "Primera revisión" &&
                await contexto.Cambios.CountAsync(c => c.EntidadId == solicitudId && c.Accion == "Antecedente.PruebaPersistencia") == 1,
                "El conflicto revierte documento e historial junto con la revisión de la solicitud.");
        }
        var nuevo = new DocumentacionSolicitud(
            solicitudId,
            "Documento de prueba que debe revertirse",
            EstadoDocumentacionSolicitud.Presentada,
            null,
            null,
            Guid.NewGuid());
        var cambioFallido = Cambio();
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            var repositorio = new RepositorioAntecedentesSolicitud(contexto);
            var solicitud = (await repositorio.ObtenerAsync(institucionId, solicitudId))!;
            solicitud.RegistrarRevisionAntecedentes();
            contexto.Cambios.Add(cambioFallido);
            contexto.Entry(cambioFallido).Property(c => c.Datos).CurrentValue = null!;
            try
            {
                await repositorio.AgregarDocumentoAsync(
                    solicitud,
                    nuevo,
                    cambioFallido);
                throw new Exception("Se guardó un documento con auditoría inválida.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "Un fallo de historial rechaza el alta documental completa.");
            }
        }
        await using (var contexto = new ContextoInscripciones(opciones))
        {
            comprobar(!await contexto.DocumentosSolicitud.AnyAsync(d => d.Id == nuevo.Id) &&
                !await contexto.Cambios.AnyAsync(c => c.Id == cambioFallido.Id) &&
                (await contexto.Solicitudes.SingleAsync(s => s.Id == solicitudId)).RevisionDatos == revisionInicial + 1,
                "El rollback no deja documento, historial ni incremento de revisión parciales.");
        }
    }
}
