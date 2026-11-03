using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPeriodos
{
    public static async Task EjecutarAsync(
        HttpClient cliente,
        Func<string, Task> iniciarSesion,
        Action<bool, string> comprobar)
    {
        var alta = await cliente.PostAsJsonAsync("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin para periodos",
            new DatosDomicilio(
                "Calle",
                "123",
                "La Plata",
                "Buenos Aires"),
            "12345"));
        var institucion = (await alta.Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;
        var ruta = $"/api/v1/instituciones/{institucion}/periodos";
        var entrada = new CrearPeriodoPeticion(
            2027,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 12, 15));
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await EstadoAsync(await cliente.PostAsJsonAsync(ruta, entrada), HttpStatusCode.Unauthorized, comprobar);
        cliente.DefaultRequestHeaders.Authorization = token;
        await EstadoAsync(await cliente.PostAsJsonAsync(ruta, entrada with { Fin = entrada.Inicio.AddDays(-1) }), HttpStatusCode.BadRequest, comprobar);
        await EstadoAsync(await cliente.PostAsJsonAsync(ruta, entrada with { Ciclo = 0 }), HttpStatusCode.BadRequest, comprobar);
        await EstadoAsync(await cliente.PostAsJsonAsync(ruta, entrada with { Inicio = default }), HttpStatusCode.BadRequest, comprobar);
        await EstadoAsync(await cliente.PostAsJsonAsync($"/api/v1/instituciones/{Guid.NewGuid()}/periodos", entrada), HttpStatusCode.NotFound, comprobar);
        var respuesta = await cliente.PostAsJsonAsync(ruta, entrada);
        await EstadoAsync(respuesta, HttpStatusCode.Created, comprobar);
        var periodoId = (await respuesta.Content.ReadFromJsonAsync<CrearPeriodoRespuesta>())!.Id;
        comprobar(respuesta.Headers.Location is not null && periodoId != Guid.Empty, "El período creado tiene Id y Location.");
        var consultar = $"/api/v1/periodos/{periodoId}";
        var periodo = (await cliente.GetFromJsonAsync<ConsultarPeriodoRespuesta>(consultar))!;
        comprobar(periodo.InstitucionId == institucion && periodo.Ciclo == 2027 && periodo.Inicio == entrada.Inicio &&
            periodo.Fin == entrada.Fin && periodo.Estado == "Preparado" && periodo.Revision == 1,
            "La consulta reconstruye institución, ciclo, fechas y estado inicial del período.");
        var lista = (await cliente.GetFromJsonAsync<ConsultarPeriodoRespuesta[]>(ruta))!;
        comprobar(lista.Length == 1 && lista[0].Id == periodoId, "Se listan los períodos de la institución.");
        await EstadoAsync(await cliente.PostAsJsonAsync(consultar + "/cerrar-recepcion", new CambiarRecepcionPeticion(1, "Cerrar sin abrir")), HttpStatusCode.Conflict, comprobar);
        await EstadoAsync(await cliente.PostAsJsonAsync(consultar + "/abrir", new CambiarRecepcionPeticion(1, " ")), HttpStatusCode.BadRequest, comprobar);
        var apertura = await cliente.PostAsJsonAsync(consultar + "/abrir", new CambiarRecepcionPeticion(1, "Apertura del prototipo"));
        await EstadoAsync(apertura, HttpStatusCode.OK, comprobar);
        var abierta = (await apertura.Content.ReadFromJsonAsync<CambiarRecepcionRespuesta>())!;
        comprobar(abierta.Estado == "RecepcionAbierta" && abierta.Revision == 2,
            "Se abre sin oferta/reglas, omisión expresamente aprobada para esta versión.");
        await EstadoAsync(await cliente.PostAsJsonAsync(consultar + "/abrir", new CambiarRecepcionPeticion(2, "Abrir nuevamente")), HttpStatusCode.Conflict, comprobar);
        await EstadoAsync(await cliente.PostAsJsonAsync(consultar + "/cerrar-recepcion", new CambiarRecepcionPeticion(1, "Edición desactualizada")), HttpStatusCode.Conflict, comprobar);
        var cierre = await cliente.PostAsJsonAsync(consultar + "/cerrar-recepcion", new CambiarRecepcionPeticion(2, "Fin de recepción"));
        await EstadoAsync(cierre, HttpStatusCode.OK, comprobar);
        comprobar((await cierre.Content.ReadFromJsonAsync<CambiarRecepcionRespuesta>())!.Estado == "RecepcionCerrada",
            "Cerrar recepción no representa finalizar la admisión.");
        await EstadoAsync(await cliente.PostAsJsonAsync(consultar + "/abrir", new CambiarRecepcionPeticion(3, "Reabrir")), HttpStatusCode.Conflict, comprobar);
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>($"/api/v1/instituciones/{institucion}/historial"))!;
        comprobar(historial.Count(c => c.Accion.StartsWith("Periodo.")) == 3 &&
            historial.Any(c => c.Motivo == "Fin de recepción" && c.DatosAnteriores is not null),
            "Creación, apertura y cierre conservan historial sin registrar operaciones rechazadas.");
        await iniciarSesion("ajeno@prueba.example");
        await EstadoAsync(await cliente.GetAsync(consultar), HttpStatusCode.Forbidden, comprobar);
        await EstadoAsync(await cliente.PostAsJsonAsync(ruta, entrada), HttpStatusCode.Forbidden, comprobar);
        await EstadoAsync(await cliente.GetAsync(ruta), HttpStatusCode.Forbidden, comprobar);
        await iniciarSesion("root@prueba.example");
        await EstadoAsync(await cliente.GetAsync("/api/v1/periodos/" + Guid.NewGuid()), HttpStatusCode.NotFound, comprobar);
    }

    private static async Task EstadoAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode esperado,
        Action<bool, string> comprobar)
    {
        if (respuesta.StatusCode != esperado)
            throw new Exception($"Períodos: se esperaba {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        comprobar(true, $"Períodos HTTP {(int)esperado}");
    }
}
