using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasDistanciasManuales
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
                throw new Exception($"Distancias: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Distancias HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        var domicilio = new DatosDomicilio(
            "Jardín ficticio",
            "100",
            "La Plata",
            "Buenos Aires");
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardín distancias manuales",
            domicilio,
            "12345"))).Id;
        var raiz = $"/api/v1/instituciones/{institucion}";
        var aula = (await Alta<RegistrarAulaRespuesta>(raiz + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var turno = (await Alta<RegistrarTurnoRespuesta>(raiz + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var periodo = (await Alta<CrearPeriodoRespuesta>(raiz + "/periodos", new CrearPeriodoPeticion(
            2027,
            new DateOnly(
                2026,
                11,
                1),
            new DateOnly(
                2026,
                12,
                1)))).Id;
        var rutaPeriodo = $"/api/v1/periodos/{periodo}";
        await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas", new RegistrarOfertaPeticion(
            aula,
            turno,
            20,
            true,
            "Acta ficticia",
            "Relevamiento confirmado",
            [],
            "Preparar prueba"));
        var reglas = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-reglas", new PublicarReglasPeticion(
            new DateOnly(
                2027,
                6,
                30),
            "Perfil prototipo",
            "Publicar"))).Id;
        var oferta = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-oferta", new PublicarOfertaPeticion("Acta", "Publicar"))).Id;
        await Estado(await cliente.PutAsJsonAsync(rutaPeriodo + "/configuracion", new AsignarConfiguracionPeticion(
            new DatosVersionesConfiguracion(reglas, oferta),
            1,
            "Asignar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        var persona = (await Alta<RegistrarAspiranteRespuesta>(raiz + "/aspirantes", new RegistrarAspirantePeticion(
            "Persona ficticia",
            "Distancias",
            new DateOnly(
                2022,
                6,
                30)))).Id;
        var solicitud = (await Alta<RegistrarSolicitudRespuesta>(raiz + $"/periodos/{periodo}/solicitudes", new RegistrarSolicitudPeticion(
            persona,
            new DatosDomicilioSolicitud(
                "Familia ficticia",
                "10",
                "La Plata",
                "Buenos Aires"),
            turno))).Id;
        var rutaSolicitud = raiz + $"/solicitudes/{solicitud}";
        var ruta = rutaSolicitud + "/distancias";
        async Task<ConsultarDistanciaRespuesta[]> Lista() => (await cliente.GetFromJsonAsync<ConsultarDistanciaRespuesta[]>(ruta))!;
        var jardin = (await cliente.GetFromJsonAsync<ConsultarInstitucionRespuesta>(raiz))!;
        var datos = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!;
        comprobar((await Lista()).Length == 0, "Sin registro de distancia se devuelve una lista vacía, no cero metros.");
        var peticion = new RegistrarDistanciaManualPeticion(
            1234.567890123m,
            "Medición directa de prueba; valor ficticio",
            datos.RevisionDomicilio,
            jardin.RevisionUbicacion,
            0);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion with { DistanciaMetros = null }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion with { DistanciaMetros = -1 }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion with { Referencia = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion with { RevisionDomicilio = 0 }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion with { RevisionUbicacionJardin = jardin.RevisionUbicacion + 1 }), HttpStatusCode.Conflict);
        var primera = await Alta<ConsultarDistanciaRespuesta>(ruta + "/manual", peticion);
        comprobar(primera.DistanciaMetros == peticion.DistanciaMetros && primera.Origen == "ManualProvisional" && primera.EsVigente && primera.EsUltimoRegistro &&
            primera.UsuarioId != Guid.Empty && primera.FechaRegistro != default && primera.Entradas.GetProperty("CoordenadasJardin").ValueKind == System.Text.Json.JsonValueKind.Null,
            "La carga manual conserva decimales, referencia, autor y fecha sin inventar coordenadas o un cálculo de Geoapify.");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion), HttpStatusCode.Conflict);
        var segunda = await Alta<ConsultarDistanciaRespuesta>(ruta + "/manual", peticion with
        {
            DistanciaMetros = 1100.125m,
            UltimoNumeroRegistro = 1,
            Referencia = "Corrección del valor manual"
        });
        var historial = await Lista();
        comprobar(historial.Length == 2 && historial[0].Id == segunda.Id && historial[0].EsVigente &&
            historial[1].Id == primera.Id && !historial[1].EsVigente && !historial[1].EsUltimoRegistro && historial[1].DistanciaMetros == peticion.DistanciaMetros,
            "Corregir agrega otro registro y conserva el anterior, con una sola distancia actual.");
        datos = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!;
        comprobar(datos.RevisionDatos == 3 && datos.RevisionDomicilio == 1,
            "Registrar distancias cambia la revisión de datos para sus consumidores, sin cambiar el domicilio.");
        await Estado(await cliente.PutAsJsonAsync(rutaSolicitud, new CorregirSolicitudPeticion(
            datos.Domicilio with { Calle = "Domicilio corregido" },
            datos.TurnoPreferidoId,
            datos.TurnosAlternativosAceptados,
            datos.RevisionDatos,
            "Dirección corregida")), HttpStatusCode.OK);
        comprobar((await Lista()).All(m => !m.EsVigente), "Cambiar el domicilio invalida las distancias anteriores sin modificarlas.");
        var impacto = (await cliente.GetFromJsonAsync<ConsultarImpactoSolicitudRespuesta>(rutaSolicitud + "/impacto"))!;
        comprobar(impacto.UsosDesactualizados.Count(r => r.Tipo == "MedicionDistancia") == 2,
            "La consulta de impacto reconoce las referencias reales de las dos distancias registradas.");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion with { UltimoNumeroRegistro = 2 }), HttpStatusCode.Conflict);
        datos = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!;
        var terceraPeticion = peticion with { DistanciaMetros = 0, RevisionDomicilio = datos.RevisionDomicilio, UltimoNumeroRegistro = 2 };
        await Alta<ConsultarDistanciaRespuesta>(ruta + "/manual", terceraPeticion);
        comprobar((await Lista())[0].DistanciaMetros == 0 && (await Lista())[0].EsVigente,
            "Cero se guarda únicamente cuando se ingresó expresamente, nunca como sustituto de un dato faltante.");
        await Estado(await cliente.PutAsJsonAsync(raiz, new ActualizarInstitucionPeticion(
            jardin.Nombre,
            domicilio with { Calle = "Jardín trasladado" },
            jardin.TelefonoContacto,
            jardin.RevisionDatos,
            "Cambio de domicilio del jardín")), HttpStatusCode.OK);
        comprobar((await Lista()).All(m => !m.EsVigente), "Cambiar la dirección del jardín deja las mediciones históricas desactualizadas.");
        jardin = (await cliente.GetFromJsonAsync<ConsultarInstitucionRespuesta>(raiz))!;
        await aplicacion.AsignarAccesoAsync(
            "distancias@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        await iniciarSesion("distancias@prueba.example");
        await Alta<ConsultarDistanciaRespuesta>(ruta + "/manual", terceraPeticion with
        {
            DistanciaMetros = 900.25m,
            RevisionUbicacionJardin = jardin.RevisionUbicacion,
            UltimoNumeroRegistro = 3
        });
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Cerrar")), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Cerrar")), HttpStatusCode.OK);
        await Alta<ConsultarDistanciaRespuesta>(ruta + "/manual", terceraPeticion with
        {
            DistanciaMetros = 901m,
            RevisionUbicacionJardin = jardin.RevisionUbicacion,
            UltimoNumeroRegistro = 4
        });
        comprobar((await Lista()).Length == 5, "La revisión de distancias puede continuar después del cierre.");
        var cambios = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(raiz + "/personas/historial"))!;
        comprobar(cambios.Count(c => c.Accion == "Distancia.RegistroManual") == 5 && cambios.Any(c => c.Datos.Contains(primera.Id.ToString()) && c.Datos.Contains("AnteriorId")),
            "El historial registra cada carga manual y la referencia al valor anterior.");
        await Estado(await cliente.GetAsync($"/api/v1/instituciones/{Guid.NewGuid()}/solicitudes/{solicitud}/distancias"), HttpStatusCode.NotFound);
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(ruta), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/manual", peticion), HttpStatusCode.Forbidden);
        var autorizacion = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(ruta), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = autorizacion;
        await iniciarSesion("root@prueba.example");
        using var alcance = aplicacion.Services.CreateScope();
        await PruebasRevisionDomicilios.EjecutarAsync(
            cliente,
            aplicacion,
            iniciarSesion,
            institucion,
            solicitud,
            comprobar);
        await PruebasDistanciasPersistencia.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
            institucion,
            solicitud,
            comprobar);
    }
}
