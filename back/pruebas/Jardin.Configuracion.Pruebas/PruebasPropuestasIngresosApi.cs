using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPropuestasIngresosApi
{
    public static async Task EjecutarAsync(
        HttpClient cliente,
        ApiPruebas aplicacion,
        Func<string, Task> iniciarSesion,
        Action<bool, string> comprobar)
    {
        async Task Estado(HttpResponseMessage respuesta, HttpStatusCode esperado)
        {
            if (respuesta.StatusCode != esperado)
                throw new Exception($"HU18 esperaba {(int)esperado}, recibió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"HU18 HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        await iniciarSesion("root@prueba.example");
        var institucion = (await Alta<RegistrarInstitucionRespuesta>(
            "/api/v1/instituciones",
            new RegistrarInstitucionPeticion("Jardín HU18", new DatosDomicilio("Calle", "1", "Ciudad", "Provincia"), "123"))).Id;
        var recursos = $"/api/v1/instituciones/{institucion}";
        var periodo = (await Alta<CrearPeriodoRespuesta>(
            recursos + "/periodos",
            new CrearPeriodoPeticion(2027, new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1)))).Id;
        var rutaPeriodo = $"/api/v1/periodos/{periodo}";
        var aula = (await Alta<RegistrarAulaRespuesta>(recursos + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var turno = (await Alta<RegistrarTurnoRespuesta>(recursos + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var oferta = (await Alta<RegistrarOfertaRespuesta>(
            rutaPeriodo + "/ofertas",
            new RegistrarOfertaPeticion(aula, turno, 2, true, "Acta", "Relevamiento", [new DatosOcupacionInicial(5, 1)], "Carga"))).Id;
        var reglas = (await Alta<PublicarVersionRespuesta>(
            rutaPeriodo + "/versiones-reglas",
            new PublicarReglasPeticion(new DateOnly(2027, 6, 30), "Reglas", "Publicación"))).Id;
        var versionOferta = (await Alta<PublicarVersionRespuesta>(
            rutaPeriodo + "/versiones-oferta",
            new PublicarOfertaPeticion("Oferta", "Publicación"))).Id;
        await Estado(await cliente.PutAsJsonAsync(
            rutaPeriodo + "/configuracion",
            new AsignarConfiguracionPeticion(new DatosVersionesConfiguracion(reglas, versionOferta), 1, "Asignar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(
            rutaPeriodo + "/abrir",
            new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        var aspirante = (await Alta<RegistrarAspiranteRespuesta>(
            recursos + "/aspirantes",
            new RegistrarAspirantePeticion("Aspirante HU18", "Único", new DateOnly(2022, 6, 30), "12345678"))).Id;
        var solicitud = (await Alta<RegistrarSolicitudRespuesta>(
            recursos + $"/periodos/{periodo}/solicitudes",
            new RegistrarSolicitudPeticion(aspirante, new DatosDomicilioSolicitud("Familia", "2", "Ciudad", "Provincia"), turno))).Id;
        var distancia = await cliente.PostAsJsonAsync(
            recursos + $"/solicitudes/{solicitud}/distancias/manual",
            new RegistrarDistanciaManualPeticion(100.123456m, "Referencia ficticia", 1, 1, 0));
        await Estado(distancia, HttpStatusCode.Created);
        var ruta = rutaPeriodo + "/propuestas-ingresos";
        var entrada = new GenerarPropuestaIngresosPeticion("Revisión administrativa");
        await Estado(await cliente.PostAsJsonAsync(ruta, entrada), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(
            rutaPeriodo + "/cerrar-recepcion",
            new CambiarRecepcionPeticion(3, "Procesar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(ruta, new GenerarPropuestaIngresosPeticion(" ")), HttpStatusCode.BadRequest);
        var propuesta = await Alta<ConsultarPropuestaIngresosRespuesta>(ruta, entrada);
        comprobar(propuesta.Resultados.Single().Estado == "IngresoPropuesto" &&
            propuesta.Resultados.Single().TurnoId == turno && propuesta.EntradasVigentes,
            "HU18: integra solicitud real, distancia y oferta versionada con ocupación inicial.");
        comprobar(propuesta.VersionReglasId == reglas && propuesta.VersionOfertaId == versionOferta,
            "HU18: conserva las versiones utilizadas.");
        var consultada = (await cliente.GetFromJsonAsync<ConsultarPropuestaIngresosRespuesta>(
            $"/api/v1/propuestas-ingresos/{propuesta.Id}"))!;
        comprobar(consultada.Id == propuesta.Id && consultada.HuellaEntradas == propuesta.HuellaEntradas,
            "HU18: reconstruye la propuesta desde persistencia.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            var guardada = await contexto.PropuestasIngresos.SingleAsync(x => x.Id == propuesta.Id);
            using var entradasGuardadas = System.Text.Json.JsonDocument.Parse(guardada.Entradas);
            comprobar(entradasGuardadas.RootElement.TryGetProperty("Casos", out _) &&
                entradasGuardadas.RootElement.TryGetProperty("Sorteos", out _) &&
                entradasGuardadas.RootElement.TryGetProperty("Ocupaciones", out _),
                "HU18: conserva también las decisiones y ocupaciones utilizadas como entradas.");
            contexto.PropuestasIngresos.Remove(guardada);
            try
            {
                await contexto.SaveChangesAsync();
                comprobar(false, "HU18: debía impedir eliminar la versión histórica.");
            }
            catch (InvalidOperationException) { comprobar(true, "HU18: protege las propuestas históricas contra borrado accidental."); }
        }
        await Alta<ConsultarPropuestaIngresosRespuesta>(ruta, entrada);
        var historial = (await cliente.GetFromJsonAsync<ConsultarPropuestaIngresosRespuesta[]>(ruta))!;
        comprobar(historial.Length == 2 && historial.All(x => x.Resultados.Single().Estado == "IngresoPropuesto"),
            "HU18: regenerar conserva versiones y no ocupa plazas.");
        Guid reservaId;
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            comprobar(!await contexto.OcupacionesPlazas.AnyAsync(x => x.PeriodoId == periodo),
                "HU18: generar una propuesta no confirma ni reserva un lugar.");
            var reserva = new OcupacionPlaza(periodo, Guid.NewGuid(), oferta, 5, "Reserva");
            reservaId = reserva.Id;
            contexto.OcupacionesPlazas.Add(reserva);
            await contexto.SaveChangesAsync();
        }
        var desactualizada = (await cliente.GetFromJsonAsync<ConsultarPropuestaIngresosRespuesta>(
            $"/api/v1/propuestas-ingresos/{propuesta.Id}"))!;
        comprobar(!desactualizada.EntradasVigentes && desactualizada.Resultados.Single().Estado == "IngresoPropuesto",
            "HU18: una ocupación nueva invalida la vigencia informativa sin sobrescribir el histórico.");
        var espera = await Alta<ConsultarPropuestaIngresosRespuesta>(ruta, entrada);
        comprobar(espera.Resultados.Single().Estado == "EsperaPropuesta", "HU18: la reserva persistida descuenta disponibilidad.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            (await contexto.OcupacionesPlazas.SingleAsync(x => x.Id == reservaId)).ConfirmarIngreso();
            await contexto.SaveChangesAsync();
        }
        var ingreso = await Alta<ConsultarPropuestaIngresosRespuesta>(ruta, entrada);
        comprobar(ingreso.Resultados.Single().Estado == "EsperaPropuesta",
            "HU18: convertir una reserva en ingreso no crea una segunda plaza ocupada.");
        await aplicacion.AsignarAccesoAsync("propuestas@prueba.example", institucion, RolesPersonal.Administrativo);
        await iniciarSesion("propuestas@prueba.example");
        await Alta<ConsultarPropuestaIngresosRespuesta>(ruta, entrada);
        await iniciarSesion("propuestas-ajeno@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(ruta, entrada), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync($"/api/v1/propuestas-ingresos/{propuesta.Id}"), HttpStatusCode.Forbidden);
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(ruta), HttpStatusCode.Unauthorized);
        await iniciarSesion("root@prueba.example");
    }
}
