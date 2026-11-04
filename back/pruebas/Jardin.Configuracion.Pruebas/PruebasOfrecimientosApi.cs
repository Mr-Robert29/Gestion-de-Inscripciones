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

public static class PruebasOfrecimientosApi
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
                throw new Exception($"HU21 esperaba {(int)esperado}; recibió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"HU21 HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        await iniciarSesion("root@prueba.example");
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones",
            new RegistrarInstitucionPeticion("Jardín HU21", new DatosDomicilio("Calle", "1", "Ciudad", "Provincia"), "123"))).Id;
        var recursos = $"/api/v1/instituciones/{institucion}";
        var periodo = (await Alta<CrearPeriodoRespuesta>(recursos + "/periodos",
            new CrearPeriodoPeticion(2027, new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1)))).Id;
        var rutaPeriodo = $"/api/v1/periodos/{periodo}";
        var aula = (await Alta<RegistrarAulaRespuesta>(recursos + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var manana = (await Alta<RegistrarTurnoRespuesta>(recursos + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var tarde = (await Alta<RegistrarTurnoRespuesta>(recursos + "/turnos", new RegistrarTurnoPeticion("Tarde"))).Id;
        var oferta = (await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas",
            new RegistrarOfertaPeticion(aula, manana, 1, true, "Acta", "Relevamiento", [], "Carga"))).Id;
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
                new RegistrarAspirantePeticion(nombre, "Prueba HU21", new DateOnly(2027 - edad, 6, 30)))).Id;
            var solicitud = (await Alta<RegistrarSolicitudRespuesta>(recursos + $"/periodos/{periodo}/solicitudes",
                new RegistrarSolicitudPeticion(aspirante, new DatosDomicilioSolicitud("Dirección", "2", "Ciudad", "Provincia"), turno,
                    turno == tarde ? [manana] : []))).Id;
            await Alta<ConsultarDistanciaRespuesta>(recursos + $"/solicitudes/{solicitud}/distancias/manual",
                new RegistrarDistanciaManualPeticion(metros, "Referencia ficticia", 1, 1, 0));
            return solicitud;
        }
        var admitido = await Registrar("Admitido", 5, 10m, manana);
        var primero = await Registrar("Espera alternativa", 5, 20m, tarde);
        var segundo = await Registrar("Espera preferida", 5, 30m, manana);
        var menor = await Registrar("Otra edad", 4, 1m, manana);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Procesar")), HttpStatusCode.OK);
        var propuesta = await Alta<ConsultarPropuestaIngresosRespuesta>(rutaPeriodo + "/propuestas-ingresos", new GenerarPropuestaIngresosPeticion("Propuesta HU21"));
        var confirmar = await cliente.PostAsJsonAsync($"/api/v1/propuestas-ingresos/{propuesta.Id}/confirmacion", new ConfirmarPropuestaIngresosPeticion(propuesta.HuellaEntradas));
        await Estado(confirmar, HttpStatusCode.OK);
        var ingreso = (await confirmar.Content.ReadFromJsonAsync<DatosConfirmacionIngreso[]>())!.Single();
        var nuevaSeccion = await cliente.PostAsJsonAsync(rutaPeriodo + "/secciones",
            new DefinirSeccionPeticion(periodo, aula, manana, "Pura de cinco", [5]));
        await Estado(nuevaSeccion, HttpStatusCode.Created);
        var seccionId = (await nuevaSeccion.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSecciones>();
            contexto.AsignacionesSeccion.Add(new AsignacionSeccion(ingreso.Id, seccionId, ingreso.UsuarioId, DateTimeOffset.UtcNow));
            await contexto.SaveChangesAsync();
        }
        var candidatosRuta = rutaPeriodo + $"/ofertas/{oferta}/candidatos";
        comprobar((await cliente.GetFromJsonAsync<ConsultarCandidatosVacanteRespuesta>(candidatosRuta))!.Candidatos.Count == 0,
            "HU21: una plaza ocupada no ofrece candidatos como si estuviera libre.");
        var bajaEntrada = new RegistrarBajaIngresoPeticion(ingreso.Revision, "Retiro comunicado por la familia");
        aplicacion.FallarBajaSecciones = true;
        var bajaPendiente = await cliente.PostAsJsonAsync($"/api/v1/ingresos/{ingreso.Id}/baja", bajaEntrada);
        await Estado(bajaPendiente, HttpStatusCode.Accepted);
        var pendiente = (await bajaPendiente.Content.ReadFromJsonAsync<RegistrarBajaIngresoRespuesta>())!;
        comprobar(!pendiente.PlazaLiberada && pendiente.Estado == "PendienteCoordinacion", "HU21: el fallo externo conserva una baja durable y la plaza ocupada.");
        comprobar((await cliente.GetFromJsonAsync<ConsultarCandidatosVacanteRespuesta>(candidatosRuta))!.Candidatos.Count == 0,
            "HU21: no publica la vacante antes de dar de baja la asignación.");
        aplicacion.FallarBajaSecciones = false;
        var baja = await cliente.PostAsJsonAsync($"/api/v1/ingresos/{ingreso.Id}/baja", bajaEntrada);
        await Estado(baja, HttpStatusCode.OK);
        var completada = (await baja.Content.ReadFromJsonAsync<RegistrarBajaIngresoRespuesta>())!;
        comprobar(completada.PlazaLiberada && completada.FechaSolicitud == pendiente.FechaSolicitud,
            "HU21: reintenta sin perder autor, fecha ni motivo de la solicitud original.");
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ingresos/{ingreso.Id}/baja", bajaEntrada), HttpStatusCode.OK);
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSecciones>();
            comprobar((await contexto.AsignacionesSeccion.SingleAsync(x => x.IngresoId == ingreso.Id)).Estado == EstadoAsignacionSeccion.Baja &&
                await contexto.IngresosRetirados.CountAsync(x => x.IngresoId == ingreso.Id) == 1 &&
                await contexto.CambiosBajaAsignaciones.CountAsync(x => x.IngresoId == ingreso.Id) == 1,
                "HU21: conserva la asignación histórica dada de baja y un único retiro.");
            var repositorio = alcance.ServiceProvider.GetRequiredService<IRepositorioDistribuciones>();
            try
            {
                await repositorio.ConfirmarAsync(
                    new PropuestaDistribucion(periodo, 1, 1, 1, ingreso.UsuarioId, DateTimeOffset.UtcNow),
                    [new AsignacionSeccion(ingreso.Id, seccionId, ingreso.UsuarioId, DateTimeOffset.UtcNow)]);
                comprobar(false, "HU21: debía impedir una asignación posterior al retiro.");
            }
            catch (InvalidOperationException) { comprobar(true, "HU21: una distribución antigua no reactiva un ingreso retirado."); }
        }
        var candidatos = (await cliente.GetFromJsonAsync<ConsultarCandidatosVacanteRespuesta>(candidatosRuta))!;
        comprobar(candidatos.Candidatos.Select(x => x.SolicitudId).SequenceEqual([primero, segundo]) &&
            candidatos.Candidatos.All(x => x.SolicitudId != menor), "HU21: respeta espera y composición pura al mostrar candidatos compatibles.");
        OfrecerVacantePeticion Entrada(Guid solicitud)
            => new(Guid.NewGuid(), solicitud, oferta, propuesta.Id, "Ofrecimiento justificado");
        var concurrentes = await Task.WhenAll(
            cliente.PostAsJsonAsync(rutaPeriodo + "/ofrecimientos", Entrada(primero)),
            cliente.PostAsJsonAsync(rutaPeriodo + "/ofrecimientos", Entrada(segundo)));
        comprobar(concurrentes.Count(x => x.StatusCode == HttpStatusCode.Created) == 1 &&
            concurrentes.Count(x => x.StatusCode == HttpStatusCode.Conflict) == 1, "HU21: dos ofrecimientos concurrentes no reservan el mismo último lugar.");
        var ganador = (await concurrentes.Single(x => x.StatusCode == HttpStatusCode.Created).Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!;
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ganador.Id}/respuestas",
            new ResponderOfrecimientoPeticion(ganador.Revision, "Cancelado", "Cancelar prueba concurrente")), HttpStatusCode.OK);
        await aplicacion.AsignarAccesoAsync("propuestas@prueba.example", institucion, RolesPersonal.Administrativo);
        await iniciarSesion("propuestas@prueba.example");
        var entrada = Entrada(primero);
        var ofrecido = await Alta<ConsultarOfrecimientoRespuesta>(rutaPeriodo + "/ofrecimientos", entrada);
        var repetido = await Alta<ConsultarOfrecimientoRespuesta>(rutaPeriodo + "/ofrecimientos", entrada);
        comprobar(repetido.Id == ofrecido.Id && repetido.Historial.Count == 1, "HU21: la misma operación no duplica ofrecimiento, reserva o historial.");
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/ofrecimientos", entrada with { Motivo = "Otro" }), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ingresos/{ingreso.Id}/baja", bajaEntrada), HttpStatusCode.Forbidden);
        comprobar((await cliente.GetFromJsonAsync<ConsultarCandidatosVacanteRespuesta>(candidatosRuta))!.Candidatos.Count == 0,
            "HU21: una reserva activa consume el lugar disponible.");
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/respuestas",
            new ResponderOfrecimientoPeticion(1, "Rechazado", "La familia no acepta el alternativo")), HttpStatusCode.OK);
        var listaRuta = recursos + $"/periodos/{periodo}/lista-espera";
        comprobar((await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!.EnEspera.Resultados
            .Any(x => x.SolicitudId == primero && x.Estado == "EnEspera" && x.TurnoPreferidoId == tarde),
            "HU21: rechazar el turno alternativo no retira al aspirante de su espera.");
        ofrecido = await Alta<ConsultarOfrecimientoRespuesta>(rutaPeriodo + "/ofrecimientos", Entrada(primero));
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/respuestas",
            new ResponderOfrecimientoPeticion(1, "Aceptado", "Acepta", null)), HttpStatusCode.BadRequest);
        var aceptarEntrada = new ResponderOfrecimientoPeticion(1, "Aceptado", "Acepta", "Acta familiar HU21");
        var aceptacion = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/respuestas", aceptarEntrada);
        await Estado(aceptacion, HttpStatusCode.OK);
        var aceptado = (await aceptacion.Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!;
        var repeticionAceptacion = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/respuestas", aceptarEntrada);
        await Estado(repeticionAceptacion, HttpStatusCode.OK);
        comprobar((await repeticionAceptacion.Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!.Historial.Count == 2,
            "HU21: el reintento de aceptación conserva una única evidencia.");
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/confirmacion",
            new ConfirmarOfrecimientoPeticion(aceptado.Revision, "Confirmar")), HttpStatusCode.Forbidden);
        var cancelada = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/respuestas",
            new ResponderOfrecimientoPeticion(aceptado.Revision, "Cancelado", "Cancelación posterior"));
        await Estado(cancelada, HttpStatusCode.OK);
        comprobar((await cancelada.Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!.Historial
            .Any(x => x.EstadoNuevo == "Aceptado" && x.Referencia == "Acta familiar HU21"),
            "HU21: cancelar no borra la evidencia de la aceptación anterior.");
        await iniciarSesion("root@prueba.example");
        ofrecido = await Alta<ConsultarOfrecimientoRespuesta>(rutaPeriodo + "/ofrecimientos", Entrada(primero));
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/confirmacion",
            new ConfirmarOfrecimientoPeticion(1, "Sin aceptación")), HttpStatusCode.Conflict);
        var aceptacionFinal = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/respuestas", aceptarEntrada);
        await Estado(aceptacionFinal, HttpStatusCode.OK);
        async Task CambiarComposicion(int edad)
        {
            using var alcance = aplicacion.Services.CreateScope();
            var repositorio = alcance.ServiceProvider.GetRequiredService<IRepositorioSecciones>();
            var seccion = (await repositorio.ObtenerPorIdAsync(seccionId))!;
            seccion.CambiarComposicion([edad], "Composición de prueba", [[3], [4], [5], [3, 4], [4, 5]]);
            await repositorio.ActualizarAsync(seccion);
        }
        await CambiarComposicion(4);
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/confirmacion",
            new ConfirmarOfrecimientoPeticion(2, "Revalidar")), HttpStatusCode.Conflict);
        await CambiarComposicion(5);
        var confirmadoHttp = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/confirmacion", new ConfirmarOfrecimientoPeticion(2, "Ingreso desde espera"));
        await Estado(confirmadoHttp, HttpStatusCode.OK);
        var confirmado = (await confirmadoHttp.Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!;
        var reintento = await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/confirmacion", new ConfirmarOfrecimientoPeticion(2, "Ingreso desde espera"));
        await Estado(reintento, HttpStatusCode.OK);
        comprobar((await reintento.Content.ReadFromJsonAsync<ConsultarOfrecimientoRespuesta>())!.IngresoId == confirmado.IngresoId,
            "HU21: repetir confirmación devuelve el mismo ingreso.");
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/ofrecimientos/{ofrecido.Id}/confirmacion",
            new ConfirmarOfrecimientoPeticion(2, "Otro motivo")), HttpStatusCode.Conflict);
        var listaFinal = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!;
        comprobar(listaFinal.EnEspera.Resultados.All(x => x.SolicitudId != primero && x.SolicitudId != admitido) && listaFinal.TotalEnEspera == 2,
            "HU21: actualiza la espera sin reinsertar al alumno dado de baja.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            comprobar(await contexto.IngresosConfirmados.CountAsync(x => x.PeriodoId == periodo) == 2 &&
                await contexto.IngresosConfirmados.CountAsync(x => x.PeriodoId == periodo && x.Activo) == 1 &&
                await contexto.OcupacionesPlazas.CountAsync(x => x.PeriodoId == periodo && x.Activa) == 1 &&
                (await contexto.OcupacionesPlazas.SingleAsync(x => x.Id == confirmado.ReservaId)).Tipo == "Ingreso",
                "HU21: reserva convertida, un único ingreso activo y sin doble consumo de plaza.");
        }
        await iniciarSesion("propuestas-ajeno@prueba.example");
        await Estado(await cliente.GetAsync($"/api/v1/ofrecimientos/{confirmado.Id}"), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(candidatosRuta), HttpStatusCode.Forbidden);
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(candidatosRuta), HttpStatusCode.Unauthorized);
        await iniciarSesion("root@prueba.example");
    }
}
