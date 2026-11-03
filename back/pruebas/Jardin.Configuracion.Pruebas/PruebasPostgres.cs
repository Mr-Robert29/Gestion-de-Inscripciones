using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Application.Servicios;
using Jardin.Configuracion.Application.Seguridad;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Domain.Entidades;
using Jardin.Configuracion.Domain.ObjetosDeValor;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPostgres
{
    public static async Task EjecutarAsync(string conexion, Action<bool, string> comprobar)
    {
        var configuracionConexion = new Npgsql.NpgsqlConnectionStringBuilder(conexion);
        if (configuracionConexion.Database?.StartsWith("hu01_pruebas_", StringComparison.Ordinal) != true)
            throw new InvalidOperationException("Las pruebas PostgreSQL requieren una base exclusiva cuyo nombre comience con hu01_pruebas_.");
        var opciones = new DbContextOptionsBuilder<ContextoConfiguracion>()
            .UseNpgsql(conexion, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "configuracion")).Options;
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            await contexto.Database.MigrateAsync();
        }
        await ComprobarPersistenciaAsync(opciones, comprobar);
    }

    public static async Task ComprobarPersistenciaAsync(DbContextOptions<ContextoConfiguracion> opciones, Action<bool, string> comprobar)
    {
        var autor = Guid.NewGuid();
        var institucion = new Institucion(
            "Prueba persistente",
            new Domicilio(
                "Calle de prueba",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345",
            coordenadas: new Coordenadas(-34, -58));
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            await new RepositorioInstituciones(contexto).AgregarAsync(institucion, Cambio(institucion, autor));
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var leida = await contexto.Instituciones.SingleAsync(i => i.Id == institucion.Id);
            comprobar(leida.Coordenadas?.Latitud == -34 && leida.Domicilio.Numero == "1", "Reconstrucción persistente de objetos de valor.");
        }

        await using var primero = new ContextoConfiguracion(opciones);
        await using var segundo = new ContextoConfiguracion(opciones);
        var a = (await primero.Instituciones.SingleAsync(i => i.Id == institucion.Id));
        var b = (await segundo.Instituciones.SingleAsync(i => i.Id == institucion.Id));
        a.ActualizarContacto("111", null);
        b.ActualizarContacto("222", null);
        await new RepositorioInstituciones(primero).ActualizarAsync(a, Cambio(a, autor));
        try
        {
            await new RepositorioInstituciones(segundo).ActualizarAsync(b, Cambio(b, autor));
            throw new Exception("Se permitió sobrescribir una edición concurrente.");
        }
        catch (ConflictoInstitucionException)
        {
            comprobar(true, "Dos contextos concurrentes no sobrescriben la edición anterior.");
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var cantidad = await contexto.Cambios.CountAsync(c => c.InstitucionId == institucion.Id);
            comprobar(cantidad == 2, "Una escritura concurrente rechazada no agrega historial.");
            var leida = await contexto.Instituciones.SingleAsync(i => i.Id == institucion.Id);
            leida.ActualizarContacto("333", null);
            var cambioInvalido = new CambioInstitucion(
                Guid.NewGuid(),
                autor,
                "Prueba",
                "Fallo de FK simulado",
                null,
                "{}");
            try
            {
                await new RepositorioInstituciones(contexto).ActualizarAsync(leida, cambioInvalido);
                throw new Exception("Se permitió un historial inválido.");
            }
            catch (DbUpdateException)
            {
                comprobar(true, "Un fallo de historial rechaza el guardado conjunto.");
            }
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            comprobar((await contexto.Instituciones.SingleAsync(i => i.Id == institucion.Id)).TelefonoContacto == "111",
                "La transacción revierte los datos cuando falla el historial.");
        }

        await PruebasOfertaPersistencia.EjecutarAsync(
            opciones,
            institucion.Id,
            comprobar);

        var referencia = new DatosUsoUbicacion(Guid.NewGuid(), "PropuestaIngreso", 1);
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var servicio = new ServicioConfiguracion(new RepositorioInstituciones(contexto), new ContextoPrueba(autor));
            await servicio.RegistrarUsoUbicacionAsync(
                institucion.Id,
                referencia,
                default);
            await servicio.RegistrarUsoUbicacionAsync(
                institucion.Id,
                referencia,
                default);
            await servicio.RegistrarUsoUbicacionAsync(
                institucion.Id,
                new DatosUsoUbicacion(Guid.NewGuid(), "MedicionDistancia", 1),
                default);
            await servicio.RegistrarUsoUbicacionAsync(
                institucion.Id,
                new DatosUsoUbicacion(Guid.NewGuid(), "PropuestaDistribucion", 1),
                default);
            comprobar(await contexto.UsosUbicacion.CountAsync(u => u.InstitucionId == institucion.Id) == 3,
                "Repetir una referencia no duplica el índice de mediciones y propuestas.");
            comprobar(await servicio.ComprobarVigenciaUsoAsync(
                institucion.Id,
                referencia,
                default), "La referencia con ubicación vigente es válida.");
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var servicio = new ServicioConfiguracion(new RepositorioInstituciones(contexto), new ContextoPrueba(autor));
            var consulta = (await servicio.ConsultarInstitucionAsync(institucion.Id))!;
            await servicio.ActualizarInstitucionAsync(
                institucion.Id,
                new ActualizarInstitucionPeticion(
                    consulta.Nombre,
                    consulta.Domicilio,
                    consulta.TelefonoContacto,
                    consulta.RevisionDatos,
                    "Corregir ubicación",
                    Coordenadas: new DatosCoordenadas(-35, -59)),
                default);
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var servicio = new ServicioConfiguracion(new RepositorioInstituciones(contexto), new ContextoPrueba(autor));
            var impacto = await servicio.ConsultarImpactoUbicacionAsync(institucion.Id);
            comprobar(impacto.ReferenciasAfectadas.Any(r => r.ReferenciaId == referencia.ReferenciaId),
                "La consulta identifica la propuesta referenciada que usó la ubicación anterior.");
            comprobar(impacto.ReferenciasAfectadas.Count == 3 && impacto.ReferenciasAfectadas.All(r => r.RevisionUbicacion == 1),
                "El impacto incluye mediciones y distribución sin sobrescribir sus revisiones originales.");
            comprobar(!await servicio.ComprobarVigenciaUsoAsync(
                institucion.Id,
                referencia,
                default), "El contrato de vigencia rechaza la referencia obsoleta.");
            var consulta = (await servicio.ConsultarInstitucionAsync(institucion.Id))!;
            await servicio.ActualizarInstitucionAsync(
                institucion.Id,
                new ActualizarInstitucionPeticion(
                    consulta.Nombre,
                    consulta.Domicilio with { Numero = "999" },
                    consulta.TelefonoContacto,
                    consulta.RevisionDatos,
                    "Dejar ubicación pendiente"),
                default);
            comprobar((await servicio.ConsultarImpactoUbicacionAsync(institucion.Id)).UbicacionPendiente,
                "Una ubicación retirada queda pendiente, no se transforma en cero.");
            var final = (await servicio.ConsultarInstitucionAsync(institucion.Id))!;
            comprobar(final.Domicilio.Numero == "999" && final.RevisionUbicacion == consulta.RevisionUbicacion + 1,
                "El cambio de domicilio conserva la identidad y aumenta la revisión una sola vez.");
        }
        var periodo = new PeriodoInscripcion(
            institucion.Id,
            2027,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 12, 15));
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            contexto.Periodos.Add(periodo);
            await contexto.SaveChangesAsync();
        }
        await using var primeraApertura = new ContextoConfiguracion(opciones);
        await using var segundaApertura = new ContextoConfiguracion(opciones);
        var periodoA = await primeraApertura.Periodos.SingleAsync(p => p.Id == periodo.Id);
        var periodoB = await segundaApertura.Periodos.SingleAsync(p => p.Id == periodo.Id);
        periodoA.Abrir();
        periodoB.Abrir();
        await new RepositorioPeriodos(primeraApertura).ActualizarAsync(periodoA, new CambioInstitucion(
            institucion.Id,
            autor,
            "PruebaPeriodo",
            "Primera apertura",
            null,
            "{}"));
        try
        {
            await new RepositorioPeriodos(segundaApertura).ActualizarAsync(periodoB, new CambioInstitucion(
                institucion.Id,
                autor,
                "PruebaPeriodo",
                "Apertura concurrente",
                null,
                "{}"));
            throw new Exception("Se permitió abrir el período con una revisión anterior.");
        }
        catch (ConflictoInstitucionException)
        {
            comprobar(true, "Los cambios concurrentes del período no se sobrescriben.");
        }
        await using (var contexto = new ContextoConfiguracion(opciones))
        {
            var guardado = await contexto.Periodos.SingleAsync(p => p.Id == periodo.Id);
            comprobar(guardado.Estado == EstadoPeriodo.RecepcionAbierta && guardado.Revision == 2,
                "El período se reconstruye con su estado y revisión persistidos.");
            comprobar(await contexto.Cambios.CountAsync(c => c.InstitucionId == institucion.Id && c.Accion == "PruebaPeriodo") == 1,
                "La apertura concurrente rechazada no duplica el historial.");
        }
    }

    private static CambioInstitucion Cambio(Institucion institucion, Guid autor)
    {
        return new CambioInstitucion(
            institucion.Id,
            autor,
            "Prueba",
            "Comprobación de persistencia",
            null,
            "{}");
    }

    private sealed class ContextoPrueba : IContextoUsuarioConfiguracion
    {
        private readonly Guid _autor;
        public ContextoPrueba(Guid autor) { _autor = autor; }

        // Implementa IContextoUsuarioConfiguracion.ObtenerAsync para datos ficticios de la prueba.
        public Task<UsuarioConfiguracion?> ObtenerAsync(CancellationToken tokenCancelacion = default)
        {
            return Task.FromResult<UsuarioConfiguracion?>(new UsuarioConfiguracion(
                _autor,
                true,
                true,
                new Dictionary<Guid, bool>()));
        }
    }
}
