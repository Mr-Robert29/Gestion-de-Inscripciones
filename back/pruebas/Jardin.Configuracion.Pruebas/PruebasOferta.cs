using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jardin.Configuracion.Contracts;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasOferta
{
    public static async Task EjecutarAsync(
        HttpClient cliente,
        Func<string, Task> iniciarSesion,
        Action<bool, string> comprobar)
    {
        async Task Estado(HttpResponseMessage respuesta, HttpStatusCode esperado)
        {
            if (respuesta.StatusCode != esperado)
                throw new Exception($"Oferta: se esperaba {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Oferta HTTP {(int)esperado}");
        }

        var institucion = (await (await cliente.PostAsJsonAsync("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin oferta",
            new DatosDomicilio(
                "Calle",
                "123",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;
        var recursos = $"/api/v1/instituciones/{institucion}";
        var periodo = (await (await cliente.PostAsJsonAsync(recursos + "/periodos", new CrearPeriodoPeticion(
            2027,
            new DateOnly(
                2026,
                11,
                1),
            new DateOnly(
                2026,
                12,
                15)))).Content.ReadFromJsonAsync<CrearPeriodoRespuesta>())!.Id;
        var ruta = $"/api/v1/periodos/{periodo}/ofertas";
        await Estado(await cliente.PostAsJsonAsync(recursos + "/aulas", new RegistrarAulaPeticion(" ")), HttpStatusCode.BadRequest);
        var altaAula = await cliente.PostAsJsonAsync(recursos + "/aulas", new RegistrarAulaPeticion("Aula 1"));
        await Estado(altaAula, HttpStatusCode.Created);
        var aula = (await altaAula.Content.ReadFromJsonAsync<RegistrarAulaRespuesta>())!.Id;
        var manana = (await (await cliente.PostAsJsonAsync(recursos + "/turnos", new RegistrarTurnoPeticion("Mañana")))
            .Content.ReadFromJsonAsync<RegistrarTurnoRespuesta>())!.Id;
        var tarde = (await (await cliente.PostAsJsonAsync(recursos + "/turnos", new RegistrarTurnoPeticion("Tarde")))
            .Content.ReadFromJsonAsync<RegistrarTurnoRespuesta>())!.Id;
        comprobar((await cliente.GetFromJsonAsync<ConsultarAulaRespuesta[]>(recursos + "/aulas"))!.Length == 1 &&
            (await cliente.GetFromJsonAsync<ConsultarTurnoRespuesta[]>(recursos + "/turnos"))!.Length == 2,
            "Los recursos se guardan y consultan por institución.");
        var peticion = new RegistrarOfertaPeticion(
            aula,
            manana,
            30,
            true,
            "Acta de capacidad del establecimiento",
            "Relevamiento manual confirmado",
            [new DatosOcupacionInicial(3, 4), new DatosOcupacionInicial(4, 2)],
            "Oferta inicial");
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Capacidad = -1 }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Capacidad = 5 }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Ocupaciones = [new DatosOcupacionInicial(3, -1)] }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Ocupaciones = [new DatosOcupacionInicial(2, 1)] }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Ocupaciones = [new DatosOcupacionInicial(3, 1), new DatosOcupacionInicial(3, 2)] }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Ocupaciones = null! }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Motivo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { ReferenciaCapacidad = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { OrigenOcupaciones = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/periodos/{Guid.NewGuid()}/ofertas", peticion), HttpStatusCode.NotFound);
        foreach (var campo in new[] { "capacidad", "disponible", "ocupaciones" })
        {
            var datos = JsonSerializer.SerializeToNode(peticion, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
            datos.Remove(campo);
            await Estado(await cliente.PostAsJsonAsync(ruta, datos), HttpStatusCode.BadRequest);
        }
        var otra = (await (await cliente.PostAsJsonAsync("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Otra institución",
            new DatosDomicilio(
                "Otra calle",
                "10",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;
        var aulaAjena = (await (await cliente.PostAsJsonAsync($"/api/v1/instituciones/{otra}/aulas", new RegistrarAulaPeticion("Ajena")))
            .Content.ReadFromJsonAsync<RegistrarAulaRespuesta>())!.Id;
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { AulaId = aulaAjena }), HttpStatusCode.BadRequest);
        var alta = await cliente.PostAsJsonAsync(ruta, peticion);
        await Estado(alta, HttpStatusCode.Created);
        var id = (await alta.Content.ReadFromJsonAsync<RegistrarOfertaRespuesta>())!.Id;
        var consulta = $"/api/v1/ofertas/{id}";
        var guardada = (await cliente.GetFromJsonAsync<ConsultarOfertaRespuesta>(consulta))!;
        comprobar(guardada.Capacidad == 30 && guardada.CapacidadNetaInicial == 24 && guardada.Ocupaciones.Count == 2 && guardada.Revision == 1,
            "Se conserva capacidad documentada mayor a 25, que no implica tamaño de sección; se restan ocupaciones explícitas.");
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { TurnoId = tarde, Ocupaciones = [] }), HttpStatusCode.Created);
        comprobar((await cliente.GetFromJsonAsync<ConsultarOfertaRespuesta[]>(ruta))!.Length == 2,
            "Una misma aula admite mañana y tarde sin duplicar la combinación del mismo turno.");
        var cambio = new ActualizarOfertaPeticion(
            20,
            true,
            "Nueva acta",
            "Corrección del relevamiento",
            [new DatosOcupacionInicial(3, 5), new DatosOcupacionInicial(5, 3)],
            1,
            "Cambio de capacidad y ocupación");
        await Estado(await cliente.PutAsJsonAsync(consulta, cambio), HttpStatusCode.OK);
        guardada = (await cliente.GetFromJsonAsync<ConsultarOfertaRespuesta>(consulta))!;
        comprobar(guardada.CapacidadNetaInicial == 12 && guardada.Revision == 2 && guardada.Ocupaciones.Single(o => o.Sala == 3).Cantidad == 5 && guardada.Ocupaciones.All(o => o.Sala != 4),
            "La actualización conserva, agrega y elimina ocupaciones correctamente al reconstruir desde otra petición.");
        await Estado(await cliente.PutAsJsonAsync(consulta, cambio), HttpStatusCode.Conflict);
        await Estado(await cliente.PutAsJsonAsync(consulta, cambio with { Revision = 2, Capacidad = 7 }), HttpStatusCode.BadRequest);
        comprobar((await cliente.GetFromJsonAsync<ConsultarOfertaRespuesta>(consulta))!.Revision == 2,
            "Un cambio inválido no altera la revisión persistida.");
        await Estado(await cliente.PutAsJsonAsync(consulta, cambio with { Revision = 2, Disponible = false, Motivo = "Suspensión de disponibilidad" }), HttpStatusCode.OK);
        comprobar((await cliente.GetFromJsonAsync<ConsultarOfertaRespuesta>(consulta))!.CapacidadNetaInicial == 0,
            "Una oferta no disponible no aporta capacidad neta inicial.");
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>(recursos + "/historial"))!;
        var actualizacion = historial.Single(h => h.Motivo == cambio.Motivo);
        comprobar(actualizacion.DatosAnteriores!.Contains("Acta de capacidad") && actualizacion.DatosNuevos.Contains("Nueva acta"),
            "El historial conserva referencia anterior, nueva y motivo del cambio.");
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(consulta), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(ruta), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(recursos + "/aulas", new RegistrarAulaPeticion("Prohibida")), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(consulta, cambio with { Revision = 3 }), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
    }
}

