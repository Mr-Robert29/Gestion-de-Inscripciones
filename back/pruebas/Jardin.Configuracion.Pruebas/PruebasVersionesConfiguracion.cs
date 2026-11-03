using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jardin.Configuracion.Contracts;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasVersionesConfiguracion
{
    public static async Task EjecutarAsync(
        HttpClient cliente,
        Func<string, Task> iniciarSesion,
        Action<bool, string> comprobar)
    {
        async Task Estado(HttpResponseMessage respuesta, HttpStatusCode esperado)
        {
            if (respuesta.StatusCode != esperado)
                throw new Exception($"Versiones: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Versiones HTTP {(int)esperado}");
        }
        async Task<Guid> Publicar(string ruta, object peticion)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, peticion);
            await Estado(respuesta, HttpStatusCode.Created);
            comprobar(respuesta.Headers.Location is not null, "Publicar devuelve la ubicación de la versión consultable.");
            return (await respuesta.Content.ReadFromJsonAsync<PublicarVersionRespuesta>())!.Id;
        }
        async Task<ConsultarVersionRespuesta> Consultar(Guid id)
            => (await cliente.GetFromJsonAsync<ConsultarVersionRespuesta>($"/api/v1/versiones-configuracion/{id}"))!;
        async Task<bool> Vigente(string ruta, DatosVersionesConfiguracion versiones)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta + "/configuracion/comprobar-vigencia", versiones);
            await Estado(respuesta, HttpStatusCode.OK);
            return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("vigente").GetBoolean();
        }

        var institucion = (await (await cliente.PostAsJsonAsync("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin versiones",
            new DatosDomicilio(
                "Calle",
                "10",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;
        var recursos = $"/api/v1/instituciones/{institucion}";
        var entradaPeriodo = new CrearPeriodoPeticion(
            2027,
            new DateOnly(
                2026,
                11,
                1),
            new DateOnly(
                2026,
                12,
                1));
        var periodo = (await (await cliente.PostAsJsonAsync(recursos + "/periodos", entradaPeriodo)).Content.ReadFromJsonAsync<CrearPeriodoRespuesta>())!.Id;
        var otroPeriodo = (await (await cliente.PostAsJsonAsync(recursos + "/periodos", entradaPeriodo with { Ciclo = 2028 })).Content.ReadFromJsonAsync<CrearPeriodoRespuesta>())!.Id;
        var ruta = $"/api/v1/periodos/{periodo}";
        var reglasEntrada = new PublicarReglasPeticion(
            new DateOnly(
                2027,
                6,
                30),
            "Reglas aprobadas para el prototipo",
            "Publicación inicial");
        var ofertaEntrada = new PublicarOfertaPeticion("Acta de prueba", "Publicación de capacidades");
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-reglas", reglasEntrada), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-reglas", reglasEntrada with { FechaCorte = default }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-reglas", reglasEntrada with { Referencia = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-reglas", reglasEntrada with { Motivo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-oferta", ofertaEntrada), HttpStatusCode.Conflict);
        var reglas = await Publicar(ruta + "/versiones-reglas", reglasEntrada);
        var primera = await Consultar(reglas);
        var datosReglas = primera.Datos.Deserialize<DatosReglasPeriodo>()!;
        comprobar(primera.Formato == 1 && primera.UsuarioId != Guid.Empty && primera.Fecha != default &&
            datosReglas.FechaCorte == reglasEntrada.FechaCorte && datosReglas.OrdenGruposEtarios.SequenceEqual([5, 4, 3]) &&
            datosReglas.ReglaDistancia.Contains("Geoapify") && datosReglas.CombinacionesEdades.Count == 5,
            "La versión conserva autor, fecha, formato y perfil explícito de reglas del prototipo.");
        var aula = (await (await cliente.PostAsJsonAsync(recursos + "/aulas", new RegistrarAulaPeticion("Aula"))).Content.ReadFromJsonAsync<RegistrarAulaRespuesta>())!.Id;
        var turno = (await (await cliente.PostAsJsonAsync(recursos + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Content.ReadFromJsonAsync<RegistrarTurnoRespuesta>())!.Id;
        var ofertaPeticion = new RegistrarOfertaPeticion(
            aula,
            turno,
            20,
            true,
            "Acta 1",
            "Relevamiento confirmado",
            [new DatosOcupacionInicial(3, 2)],
            "Oferta inicial");
        var oferta = (await (await cliente.PostAsJsonAsync(ruta + "/ofertas", ofertaPeticion)).Content.ReadFromJsonAsync<RegistrarOfertaRespuesta>())!.Id;
        var versionOferta = await Publicar(ruta + "/versiones-oferta", ofertaEntrada);
        var pareja = new DatosVersionesConfiguracion(reglas, versionOferta);
        comprobar(!await Vigente(ruta, pareja), "Publicar no asigna automáticamente una configuración al período.");
        var asignacion = new AsignarConfiguracionPeticion(
            pareja,
            1,
            "Usar reglas y oferta publicadas");
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion), HttpStatusCode.OK);
        var periodoLeido = (await cliente.GetFromJsonAsync<ConsultarPeriodoRespuesta>(ruta))!;
        comprobar(periodoLeido.VersionReglasId == reglas && periodoLeido.VersionOfertaId == versionOferta && periodoLeido.Revision == 2,
            "Consultar el período expone las versiones asignadas y su nueva revisión.");
        comprobar(await Vigente(ruta, pareja), "El par asignado está vigente.");
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion), HttpStatusCode.Conflict);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion with { Revision = 2 }), HttpStatusCode.Conflict);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion with { Revision = 2, Motivo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion with { Revision = 2, Versiones = new DatosVersionesConfiguracion(versionOferta, reglas) }), HttpStatusCode.BadRequest);
        var reglasAjenas = await Publicar($"/api/v1/periodos/{otroPeriodo}/versiones-reglas", reglasEntrada);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion with { Revision = 2, Versiones = new DatosVersionesConfiguracion(reglasAjenas, versionOferta) }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion with { Revision = 2, Versiones = new DatosVersionesConfiguracion(Guid.NewGuid(), versionOferta) }), HttpStatusCode.NotFound);
        await Estado(await cliente.PutAsJsonAsync($"/api/v1/ofertas/{oferta}", new ActualizarOfertaPeticion(
            15,
            true,
            "Acta 2",
            "Relevamiento corregido",
            [new DatosOcupacionInicial(4, 3)],
            1,
            "Cambio futuro de capacidad")), HttpStatusCode.OK);
        var anteriorOferta = (await Consultar(versionOferta)).Datos.Deserialize<ConsultarOfertaRespuesta[]>()!.Single();
        comprobar(anteriorOferta.Capacidad == 20 && anteriorOferta.Ocupaciones.Single().Sala == 3,
            "Editar la oferta no modifica la capacidad ni las ocupaciones de la versión histórica.");
        comprobar(await Vigente(ruta, pareja), "Editar el borrador no cambia las versiones asignadas silenciosamente.");
        var nuevaOferta = await Publicar(ruta + "/versiones-oferta", ofertaEntrada with { Motivo = "Publicar acta 2" });
        var nuevasReglas = await Publicar(ruta + "/versiones-reglas", reglasEntrada with { FechaCorte = reglasEntrada.FechaCorte.AddDays(1), Motivo = "Cambio explícito de fecha" });
        comprobar((await Consultar(reglas)).Datos.GetRawText() == primera.Datos.GetRawText(), "La publicación posterior conserva íntegra la versión anterior de reglas.");
        var nuevaPareja = new DatosVersionesConfiguracion(nuevasReglas, nuevaOferta);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion with { Revision = 2, Versiones = nuevaPareja }), HttpStatusCode.OK);
        comprobar(!await Vigente(ruta, pareja) && await Vigente(ruta, nuevaPareja),
            "Cambiar las versiones asignadas permite detectar referencias anteriores desactualizadas.");
        var lista = (await cliente.GetFromJsonAsync<ConsultarVersionRespuesta[]>(ruta + "/versiones-configuracion"))!;
        comprobar(lista.Length == 4 && lista.Any(v => v.Id == reglas) && lista.Any(v => v.Id == versionOferta),
            "Las versiones anteriores continúan disponibles para consulta.");
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>(recursos + "/historial"))!;
        comprobar(historial.Count(c => c.Accion == "Configuracion.Asignacion") == 2 &&
            historial.Any(c => c.Accion == "Configuracion.Asignacion" && c.DatosAnteriores!.Contains(versionOferta.ToString())),
            "El historial conserva pares anteriores y nuevos; los intentos rechazados no agregan asignaciones.");
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync($"/api/v1/versiones-configuracion/{reglas}"), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(ruta + "/versiones-configuracion"), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-reglas", reglasEntrada), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/versiones-oferta", ofertaEntrada), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", asignacion), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/configuracion/comprobar-vigencia", pareja), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        await Estado(await cliente.GetAsync($"/api/v1/versiones-configuracion/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }
}
