using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Configuracion.Infrastructure.Persistencia;
using Jardin.Configuracion.Application.Excepciones;
using Jardin.Configuracion.Domain.Entidades;
using Jardin.Usuarios.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Jardin.Configuracion.Pruebas;

public static class EjecutorPruebas
{
    private static int _comprobaciones;

    public static async Task Main()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Pruebas");
        Environment.SetEnvironmentVariable("Jwt__ClaveSecreta", "clave-ficticia-solo-pruebas-configuracion-2026");
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null && !Directory.Exists(Path.Combine(carpeta.FullName, "src", "Jardin.Api")))
            carpeta = carpeta.Parent;
        if (carpeta is null) throw new InvalidOperationException("Ejecutá las pruebas dentro de la estructura de back.");
        await using var aplicacion = new ApiPruebas(Path.Combine(carpeta.FullName, "src", "Jardin.Api"));
        await aplicacion.PrepararAsync();
        using var cliente = aplicacion.CreateClient();
        var peticion = new RegistrarInstitucionPeticion(
            " Jardin de prueba ",
            new DatosDomicilio(
                " San Martin ",
                "123",
                "La Plata",
                "Buenos Aires",
                "1900",
                "Esquina",
                "1",
                "A",
                "Centro"),
            " 12345 ",
            null,
            "contacto@prueba.example");
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion), HttpStatusCode.Unauthorized);
        await IniciarSesionAsync(cliente, "root@prueba.example");
        var alta = await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion);
        await EstadoAsync(alta, HttpStatusCode.Created);
        var identificador = (await alta.Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;
        Comprobar(identificador != Guid.Empty && alta.Headers.Location is not null, "El alta devuelve identificador y Location.");
        var actual = (await cliente.GetFromJsonAsync<ConsultarInstitucionRespuesta>($"/api/v1/instituciones/{identificador}"))!;
        Comprobar(actual.Nombre == "Jardin de prueba" && actual.Coordenadas is null && actual.RevisionUbicacion == 1,
            "La consulta reconstruye la institución sin inventar coordenadas.");
        Comprobar(actual.Domicilio.Piso == "1" && actual.Domicilio.Complemento == "Esquina", "Los datos adicionales del domicilio persisten.");
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion with { Nombre = "  " }), HttpStatusCode.BadRequest);
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion with { TelefonoContacto = "" }), HttpStatusCode.BadRequest);
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion with { Domicilio = peticion.Domicilio with { Calle = "" } }), HttpStatusCode.BadRequest);
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion with { Coordenadas = new DatosCoordenadas(91, 0) }), HttpStatusCode.BadRequest);
        var sinLongitud = new
        {
            nombre = peticion.Nombre,
            domicilio = peticion.Domicilio,
            telefonoContacto = peticion.TelefonoContacto,
            coordenadas = new { latitud = -34.6m }
        };
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", sinLongitud), HttpStatusCode.BadRequest);
        var actualizar = new ActualizarInstitucionPeticion(
            actual.Nombre,
            actual.Domicilio,
            actual.TelefonoContacto,
            actual.RevisionDatos,
            "Precisar ubicación",
            actual.Codigo,
            actual.CorreoElectronicoContacto,
            new DatosCoordenadas(-34.6m, -58.38m));
        var edicion = await cliente.PutAsJsonAsync($"/api/v1/instituciones/{identificador}", actualizar);
        await EstadoAsync(edicion, HttpStatusCode.OK);
        var resultadoEdicion = (await edicion.Content.ReadFromJsonAsync<ActualizarInstitucionRespuesta>())!;
        actual = (await cliente.GetFromJsonAsync<ConsultarInstitucionRespuesta>($"/api/v1/instituciones/{identificador}"))!;
        Comprobar(resultadoEdicion.RevisionDatos == actual.RevisionDatos, "La respuesta de edición informa la revisión guardada.");
        Comprobar(actual.Id == identificador && actual.RevisionUbicacion == 2 && actual.Coordenadas is not null,
            "La actualización conserva el Id e incrementa la revisión de ubicación.");
        await EstadoAsync(await cliente.PutAsJsonAsync($"/api/v1/instituciones/{identificador}", actualizar), HttpStatusCode.Conflict);
        await EstadoAsync(await cliente.GetAsync($"/api/v1/instituciones/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>($"/api/v1/instituciones/{identificador}/historial"))!;
        Comprobar(historial.Length == 2 && historial.All(c => c.UsuarioId != Guid.Empty) &&
            historial.Any(c => c.Motivo == "Precisar ubicación" && c.DatosAnteriores is not null), "Historial con autor, motivo y valores anteriores/nuevos.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var modulo = alcance.ServiceProvider.GetRequiredService<IModuloConfiguracion>();
            // Una llamada directa al servicio, sin identidad HTTP, tampoco puede escribir.
            try { await modulo.RegistrarInstitucionAsync(peticion); throw new Exception("Se permitió acceso sin identidad."); }
            catch (UnauthorizedAccessException) { Comprobar(true, "Application también aplica autorización."); }
        }
        await aplicacion.AsignarAccesoAsync("administrativo@prueba.example", identificador, RolesPersonal.Administrativo);
        await aplicacion.AsignarAccesoAsync("directivo@prueba.example", identificador, RolesPersonal.Directivo);
        await IniciarSesionAsync(cliente, "administrativo@prueba.example");
        await EstadoAsync(await cliente.GetAsync($"/api/v1/instituciones/{identificador}"), HttpStatusCode.OK);
        await EstadoAsync(await cliente.PostAsJsonAsync("/api/v1/instituciones", peticion), HttpStatusCode.Forbidden);
        await EstadoAsync(await cliente.PutAsJsonAsync($"/api/v1/instituciones/{identificador}", actualizar with { RevisionDatos = actual.RevisionDatos }), HttpStatusCode.Forbidden);
        await IniciarSesionAsync(cliente, "ajeno@prueba.example");
        await EstadoAsync(await cliente.GetAsync($"/api/v1/instituciones/{identificador}"), HttpStatusCode.Forbidden);
        var visibles = await cliente.GetFromJsonAsync<ConsultarInstitucionRespuesta[]>("/api/v1/instituciones");
        Comprobar(visibles!.Length == 0, "La lista no revela instituciones ajenas.");
        await IniciarSesionAsync(cliente, "directivo@prueba.example");
        await EstadoAsync(await cliente.PutAsJsonAsync($"/api/v1/instituciones/{identificador}", actualizar with
        {
            RevisionDatos = actual.RevisionDatos,
            Nombre = "Nombre corregido",
            Motivo = "Corregir nombre"
        }), HttpStatusCode.OK);
        var historialAntes = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>($"/api/v1/instituciones/{identificador}/historial"))!.Length;
        await EstadoAsync(await cliente.PutAsJsonAsync($"/api/v1/instituciones/{identificador}", actualizar with
        {
            RevisionDatos = actual.RevisionDatos + 1,
            Motivo = " "
        }), HttpStatusCode.BadRequest);
        var historialDespues = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>($"/api/v1/instituciones/{identificador}/historial"))!.Length;
        Comprobar(historialAntes == historialDespues, "Una actualización inválida no agrega historial.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>();
            var directivo = await contexto.UsuariosPersonal.Include(u => u.Accesos).SingleAsync(u => u.CorreoElectronico == "directivo@prueba.example");
            directivo.RevocarAcceso(identificador);
            await contexto.SaveChangesAsync();
        }
        await EstadoAsync(await cliente.GetAsync($"/api/v1/instituciones/{identificador}"), HttpStatusCode.Forbidden);
        await IniciarSesionAsync(cliente, "administrativo@prueba.example");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>();
            var administrativo = await contexto.UsuariosPersonal.SingleAsync(u => u.CorreoElectronico == "administrativo@prueba.example");
            administrativo.Desactivar();
            await contexto.SaveChangesAsync();
        }
        await EstadoAsync(await cliente.GetAsync($"/api/v1/instituciones/{identificador}"), HttpStatusCode.Forbidden);
        await IniciarSesionAsync(cliente, "root@prueba.example");
        await PruebasPeriodos.EjecutarAsync(
            cliente,
            correo => IniciarSesionAsync(cliente, correo),
            Comprobar);
        await PruebasOferta.EjecutarAsync(
            cliente,
            correo => IniciarSesionAsync(cliente, correo),
            Comprobar);
        await PruebasVersionesConfiguracion.EjecutarAsync(
            cliente,
            correo => IniciarSesionAsync(cliente, correo),
            Comprobar);
        await PruebasPersonasInscripciones.EjecutarAsync(
            cliente,
            aplicacion,
            correo => IniciarSesionAsync(cliente, correo),
            Comprobar);
        await PruebasSolicitudes.EjecutarAsync(
            cliente,
            aplicacion,
            correo => IniciarSesionAsync(cliente, correo),
            Comprobar);
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var cuentas = alcance.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
            var root = (await cuentas.FindByEmailAsync("root@prueba.example"))!;
            await cuentas.RemoveFromRoleAsync(root, RolesPersonal.AdministradorDelSistema);
        }
        await EstadoAsync(await cliente.GetAsync($"/api/v1/instituciones/{identificador}"), HttpStatusCode.Forbidden);
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var opciones = alcance.ServiceProvider.GetRequiredService<DbContextOptions<ContextoConfiguracion>>();
            await PruebasPostgres.ComprobarPersistenciaAsync(opciones, Comprobar);
        }
        await ComprobarPostgresAsync();
        Console.WriteLine($"Correctas: {_comprobaciones} comprobaciones. Identity/JWT real; persistencia aislada según configuración de pruebas.");
    }

    private static async Task IniciarSesionAsync(HttpClient cliente, string correo)
    {
        cliente.DefaultRequestHeaders.Authorization = null;
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/Autenticacion/iniciar-sesion", new IniciarSesionPeticion(correo, "Prueba123!"));
        await EstadoAsync(respuesta, HttpStatusCode.OK);
        var acceso = (await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacion>())!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acceso.Token);
    }

    private static async Task EstadoAsync(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        if (respuesta.StatusCode != esperado)
            throw new Exception($"HTTP esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
        Comprobar(true, $"HTTP {(int)esperado}");
    }

    private static void Comprobar(bool condicion, string descripcion)
    {
        if (!condicion) throw new Exception("Falló: " + descripcion);
        _comprobaciones++;
        Console.WriteLine("Correcto: " + descripcion);
    }

    private static async Task ComprobarPostgresAsync()
    {
        var conexion = Environment.GetEnvironmentVariable("HU01_POSTGRES_PRUEBAS");
        if (string.IsNullOrWhiteSpace(conexion))
        {
            Console.WriteLine("PostgreSQL omitido: definir HU01_POSTGRES_PRUEBAS con una base exclusiva de pruebas.");
            return;
        }
        await PruebasPostgres.EjecutarAsync(conexion, Comprobar);
    }
}
