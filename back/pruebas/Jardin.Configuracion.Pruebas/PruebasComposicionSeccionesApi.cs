using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Secciones.Application.Abstracciones;
using Jardin.Secciones.Contracts;
using Jardin.Secciones.Domain;
using Jardin.Secciones.Domain.Entidades;
using Jardin.Secciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasComposicionSeccionesApi
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
                throw new Exception($"HU26 esperaba {(int)esperado}; recibió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"HU26 HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        await iniciarSesion("root@prueba.example");
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones",
            new RegistrarInstitucionPeticion("Jardín HU26", new DatosDomicilio("Calle", "1", "Ciudad", "Provincia"), "123"))).Id;
        var recursos = $"/api/v1/instituciones/{institucion}";
        var periodo = (await Alta<CrearPeriodoRespuesta>(recursos + "/periodos",
            new CrearPeriodoPeticion(2027, new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1)))).Id;
        var rutaPeriodo = $"/api/v1/periodos/{periodo}";
        var aula = (await Alta<RegistrarAulaRespuesta>(recursos + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var manana = (await Alta<RegistrarTurnoRespuesta>(recursos + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var tarde = (await Alta<RegistrarTurnoRespuesta>(recursos + "/turnos", new RegistrarTurnoPeticion("Tarde"))).Id;
        var oferta = (await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas",
            new RegistrarOfertaPeticion(aula, manana, 2, true, "Acta", "Relevamiento", [], "Carga"))).Id;
        await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas",
            new RegistrarOfertaPeticion(aula, tarde, 0, true, "Acta", "Relevamiento", [], "Carga"));
        var reglas = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-reglas",
            new PublicarReglasPeticion(new DateOnly(2027, 6, 30), "Reglas", "Publicación"))).Id;
        var versionOferta = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-oferta",
            new PublicarOfertaPeticion("Oferta", "Publicación"))).Id;
        await Estado(await cliente.PutAsJsonAsync(rutaPeriodo + "/configuracion",
            new AsignarConfiguracionPeticion(new DatosVersionesConfiguracion(reglas, versionOferta), 1, "Asignar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        async Task<Guid> Registrar(
            string nombre,
            int edad,
            decimal metros,
            Guid turno)
        {
            var aspirante = (await Alta<RegistrarAspiranteRespuesta>(recursos + "/aspirantes",
                new RegistrarAspirantePeticion(nombre, "Prueba HU26", new DateOnly(2027 - edad, 6, 30)))).Id;
            var solicitud = (await Alta<RegistrarSolicitudRespuesta>(recursos + $"/periodos/{periodo}/solicitudes",
                new RegistrarSolicitudPeticion(aspirante, new DatosDomicilioSolicitud("Dirección", "2", "Ciudad", "Provincia"), turno,
                    turno == tarde ? [manana] : []))).Id;
            await Alta<ConsultarDistanciaRespuesta>(recursos + $"/solicitudes/{solicitud}/distancias/manual",
                new RegistrarDistanciaManualPeticion(metros, "Referencia ficticia", 1, 1, 0));
            return solicitud;
        }
        var admitido = await Registrar("Admitido", 5, 10m, manana);
        var primero = await Registrar("Segundo admitido", 5, 20m, manana);
        var segundo = await Registrar("Espera preferida", 5, 30m, manana);
        var menor = await Registrar("Otra edad", 4, 1m, manana);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Procesar")), HttpStatusCode.OK);
        var propuesta = await Alta<ConsultarPropuestaIngresosRespuesta>(rutaPeriodo + "/propuestas-ingresos", new GenerarPropuestaIngresosPeticion("Propuesta HU26"));
        var confirmar = await cliente.PostAsJsonAsync($"/api/v1/propuestas-ingresos/{propuesta.Id}/confirmacion", new ConfirmarPropuestaIngresosPeticion(propuesta.HuellaEntradas));
        await Estado(confirmar, HttpStatusCode.OK);
        var ingresosConfirmados = (await confirmar.Content.ReadFromJsonAsync<DatosConfirmacionIngreso[]>())!;
        var ingreso = ingresosConfirmados.First();
        var nuevaSeccion = await cliente.PostAsJsonAsync(rutaPeriodo + "/secciones",
            new DefinirSeccionPeticion(periodo, aula, manana, "Pura de cinco", [5]));
        await Estado(nuevaSeccion, HttpStatusCode.Created);
        var seccionId = (await nuevaSeccion.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSecciones>();
            foreach (var activo in ingresosConfirmados)
                contexto.AsignacionesSeccion.Add(new AsignacionSeccion(activo.Id, seccionId, activo.UsuarioId, DateTimeOffset.UtcNow));
            await contexto.SaveChangesAsync();
        }
        var ruta = rutaPeriodo + $"/secciones/{seccionId}/composicion";
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Sin plaza", 1)), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ingresos/{ingreso.Id}/baja",
            new RegistrarBajaIngresoPeticion(ingreso.Revision, "Liberar vacante HU26")), HttpStatusCode.OK);
        Guid borradorId;
        Guid historicaId;
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSecciones>();
            var borrador = new PropuestaDistribucion(periodo, 1, 1, 1, ingreso.UsuarioId, DateTimeOffset.UtcNow);
            var historica = new PropuestaDistribucion(periodo, 2, 1, 1, ingreso.UsuarioId, DateTimeOffset.UtcNow);
            historica.Confirmar(ingreso.UsuarioId, DateTimeOffset.UtcNow);
            contexto.PropuestasDistribucion.AddRange(borrador, historica);
            await contexto.SaveChangesAsync();
            borradorId = borrador.Id;
            historicaId = historica.Id;
        }
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([3, 5], "Combinación no permitida", 1)), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([3, 4], "Excluir edad de alumnos", 1)), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "", 1)), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Sin revisión")), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + $"/secciones/{Guid.NewGuid()}/composicion", new CambiarComposicionSeccionPeticion([4, 5], "Sección inexistente", 1)), HttpStatusCode.NotFound);
        comprobar((await cliente.GetFromJsonAsync<DatosCambioComposicion[]>(ruta + "/historial"))!.Length == 0,
            "HU26: los cambios rechazados no dejan historial parcial.");
        var cambiosConcurrentes = await Task.WhenAll(
            cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Ampliar edades ante baja", 1)),
            cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Ampliar edades ante baja", 1)));
        comprobar(cambiosConcurrentes.Count(x => x.StatusCode == HttpStatusCode.OK) == 1 &&
            cambiosConcurrentes.Count(x => x.StatusCode == HttpStatusCode.Conflict) == 1,
            "HU26: dos cambios concurrentes de la misma revisión no duplican la composición ni el historial.");
        var cambio = cambiosConcurrentes.Single(x => x.StatusCode == HttpStatusCode.OK);
        await Estado(cambio, HttpStatusCode.OK);
        var resultado = (await cambio.Content.ReadFromJsonAsync<CambiarComposicionSeccionRespuesta>())!;
        comprobar(resultado.Revision == 2 && resultado.EdadesAdmitidas.SequenceEqual([4, 5]) && resultado.PropuestasParaRevision.Contains(borradorId),
            "HU26: amplía la composición e identifica propuestas que requieren revisión.");
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Revisión antigua", 1)), HttpStatusCode.Conflict);
        var historial = (await cliente.GetFromJsonAsync<DatosCambioComposicion[]>(ruta + "/historial"))!;
        comprobar(historial.Length == 1 && historial[0].EdadesAnteriores == "5" && historial[0].EdadesNuevas == "4,5" &&
            historial[0].UsuarioId == ingreso.UsuarioId && historial[0].Motivo == "Ampliar edades ante baja" && historial[0].Fecha != default,
            "HU26: conserva composición anterior/nueva, revisiones, actor, fecha y motivo sin duplicación.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSecciones>();
            comprobar((await contexto.PropuestasDistribucion.SingleAsync(x => x.Id == borradorId)).Estado == EstadoPropuestaDistribucion.Desactualizada &&
                (await contexto.PropuestasDistribucion.SingleAsync(x => x.Id == historicaId)).Estado == EstadoPropuestaDistribucion.Confirmada,
                "HU26: marca borradores para revisión y conserva intacta la distribución histórica.");
            comprobar(await contexto.AsignacionesSeccion.CountAsync(x => x.SeccionId == seccionId && x.Estado == EstadoAsignacionSeccion.Activa) == 1,
                "HU26: conserva al alumno de cinco ya asignado sin crear nuevas asignaciones.");
            var modulo = alcance.ServiceProvider.GetRequiredService<IModuloSecciones>();
            try
            {
                await modulo.ConfirmarDistribucionAsync(borradorId, ingreso.UsuarioId);
                comprobar(false, "HU26: no debe confirmar un borrador afectado por el cambio.");
            }
            catch (InvalidOperationException)
            {
                comprobar(true, "HU26: impide confirmar la propuesta marcada para revisión.");
            }
            var vacantes = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            comprobar(await vacantes.IngresosConfirmados.CountAsync(x => x.PeriodoId == periodo && x.Activo) == 1,
                "HU26: cambiar composición no admite automáticamente al aspirante de cuatro.");
        }
        var candidatos = (await cliente.GetFromJsonAsync<ConsultarCandidatosVacanteRespuesta>(rutaPeriodo + $"/ofertas/{oferta}/candidatos"))!;
        comprobar(candidatos.Candidatos.Any(x => x.SolicitudId == menor), "HU26: el aspirante de cuatro queda habilitado como candidato compatible.");
        var ofrecimiento = await Alta<ConsultarOfrecimientoRespuesta>(rutaPeriodo + "/ofrecimientos",
            new OfrecerVacantePeticion(Guid.NewGuid(), menor, oferta, propuesta.Id, "Ofrecer plaza multiedad"));
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecimiento.Id}/confirmacion",
            new ConfirmarOfrecimientoPeticion(ofrecimiento.Revision, "Sin aceptación")), HttpStatusCode.Conflict);
        var aceptada = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecimiento.Id}/respuestas",
            new ResponderOfrecimientoPeticion(ofrecimiento.Revision, "Aceptado", "Aceptación familiar", "Acta HU26"));
        await Estado(aceptada, HttpStatusCode.OK);
        var respuestaFamiliar = (await aceptada.Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!;
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecimiento.Id}/confirmacion",
            new ConfirmarOfrecimientoPeticion(respuestaFamiliar.Revision, "Confirmar ingreso multiedad")), HttpStatusCode.OK);
        await aplicacion.AsignarAccesoAsync("propuestas@prueba.example", institucion, RolesPersonal.Administrativo);
        await iniciarSesion("propuestas@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Sin permiso", 2)), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(ruta + "/historial"), HttpStatusCode.Forbidden);
        await iniciarSesion("directivo@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(ruta, new CambiarComposicionSeccionPeticion([4, 5], "Otra institución", 2)), HttpStatusCode.Forbidden);
        await aplicacion.AsignarAccesoAsync("directivo@prueba.example", institucion, RolesPersonal.Directivo);
        await Estado(await cliente.GetAsync(ruta + "/historial"), HttpStatusCode.OK);
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(ruta + "/historial"), HttpStatusCode.Unauthorized);
        await iniciarSesion("root@prueba.example");
    }
}

