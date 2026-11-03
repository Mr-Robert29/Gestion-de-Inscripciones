using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Vacantes.Contracts;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasSorteos
{
    public static async Task EjecutarAsync(
        HttpClient cliente,
        Action<bool, string> comprobar)
    {
        var institucionRespuesta = await cliente.PostAsJsonAsync(
            "/api/v1/instituciones",
            new RegistrarInstitucionPeticion(
                "Jardín para sorteos",
                new DatosDomicilio("Calle", "123", "La Plata", "Buenos Aires"),
                "12345"));
        comprobar(institucionRespuesta.StatusCode == HttpStatusCode.Created, "Se crea la institución de prueba para sorteos.");
        var institucion = (await institucionRespuesta.Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;

        var periodoRespuesta = await cliente.PostAsJsonAsync(
            $"/api/v1/instituciones/{institucion}/periodos",
            new CrearPeriodoPeticion(
                2027,
                new DateOnly(2026, 11, 1),
                new DateOnly(2026, 12, 15)));
        comprobar(periodoRespuesta.StatusCode == HttpStatusCode.Created, "Se crea el período de prueba para sorteos.");
        var periodo = (await periodoRespuesta.Content.ReadFromJsonAsync<CrearPeriodoRespuesta>())!.Id;

        var ruta = $"/api/v1/periodos/{periodo}/sorteos";
        var primero = await cliente.PostAsJsonAsync(
            ruta,
            new RegistrarSorteoPeticion(0, new DateOnly(2026, 12, 1), "Acta Lotería 001", 3));
        if (primero.StatusCode != HttpStatusCode.Created)
            throw new Exception($"Sorteos: se esperaba 201, recibido {(int)primero.StatusCode}: {await primero.Content.ReadAsStringAsync()}");
        comprobar(true, "Se registra el número mínimo del sorteo.");
        var datoPrimero = (await primero.Content.ReadFromJsonAsync<DatosSorteo>())!;
        comprobar(datoPrimero.Numero == "000" && datoPrimero.Sala == 3, "El número se devuelve con tres dígitos y conserva la sala.");

        var invalido = await cliente.PostAsJsonAsync(
            ruta,
            new RegistrarSorteoPeticion(1000, new DateOnly(2026, 12, 1), "Número inválido", 3));
        comprobar(invalido.StatusCode == HttpStatusCode.BadRequest, "Se rechaza un número fuera del intervalo.");

        var reemplazo = await cliente.PostAsJsonAsync(
            ruta,
            new RegistrarSorteoPeticion(347, new DateOnly(2026, 12, 2), "Acta Lotería 002", 3));
        comprobar(reemplazo.StatusCode == HttpStatusCode.Created, "Se registra una nueva versión del sorteo.");
        var datoReemplazo = (await reemplazo.Content.ReadFromJsonAsync<DatosSorteo>())!;
        comprobar(datoReemplazo.Numero == "347" && datoReemplazo.Version == 2, "La nueva versión queda vigente.");

        var vigentes = await cliente.GetFromJsonAsync<DatosSorteo[]>(ruta);
        comprobar(vigentes!.Length == 1 && vigentes[0].Numero == "347", "La consulta devuelve solo el sorteo vigente por sala.");

        var salaInvalida = await cliente.PostAsJsonAsync(
            ruta,
            new RegistrarSorteoPeticion(999, new DateOnly(2026, 12, 3), "Sala inválida", 2));
        comprobar(salaInvalida.StatusCode == HttpStatusCode.BadRequest, "Se rechaza una sala fuera del rango permitido.");
    }
}
