using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Jardin.Usuarios.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasPersonasInscripciones
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
                throw new Exception($"Personas: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Personas HTTP {(int)esperado}");
        }
        async Task<Guid> CrearInstitucion(string nombre)
        {
            var respuesta = await cliente.PostAsJsonAsync("/api/v1/instituciones", new RegistrarInstitucionPeticion(
                nombre,
                new DatosDomicilio(
                    "Calle ficticia",
                    "1",
                    "La Plata",
                    "Buenos Aires"),
                "12345"));
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<RegistrarInstitucionRespuesta>())!.Id;
        }
        var institucion = await CrearInstitucion("Jardin personas");
        var otraInstitucion = await CrearInstitucion("Otra institución personas");
        var ruta = $"/api/v1/instituciones/{institucion}";
        var otraRuta = $"/api/v1/instituciones/{otraInstitucion}";
        var entrada = new RegistrarAspirantePeticion(
            "  Ana  ",
            "  Prueba  ",
            new DateOnly(
                2022,
                4,
                5));
        var adulto = new RegistrarResponsablePeticion(
            "  Juana  ",
            "Prueba",
            " 12345 ",
            "00012345",
            "DNI");
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PostAsJsonAsync(ruta + "/aspirantes", entrada), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/instituciones/{Guid.NewGuid()}/aspirantes", entrada), HttpStatusCode.NotFound);
        await aplicacion.AsignarAccesoAsync(
            "inscripciones@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        await iniciarSesion("inscripciones@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/aspirantes", entrada with { Nombre = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/aspirantes", entrada with { Apellido = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/aspirantes", entrada with { FechaNacimiento = default }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/responsables", adulto with { Nombre = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/responsables", adulto with { Telefono = " " }), HttpStatusCode.BadRequest);
        var alta = await cliente.PostAsJsonAsync(ruta + "/aspirantes", entrada);
        await Estado(alta, HttpStatusCode.Created);
        var aspiranteId = (await alta.Content.ReadFromJsonAsync<RegistrarAspiranteRespuesta>())!.Id;
        comprobar(alta.Headers.Location is not null, "El aspirante registrado tiene Id y Location consultable.");
        var consulta = ruta + $"/aspirantes/{aspiranteId}";
        var aspirante = (await cliente.GetFromJsonAsync<ConsultarAspiranteRespuesta>(consulta))!;
        comprobar(aspirante.Nombre == "Ana" && aspirante.Apellido == "Prueba" && aspirante.Dni is null &&
            aspirante.FechaNacimiento == entrada.FechaNacimiento && aspirante.Responsables.Count == 0,
            "Se guarda el aspirante sin inventar DNI, dirección, solicitud ni responsables.");
        var altaAdulto = await cliente.PostAsJsonAsync(ruta + "/responsables", adulto);
        await Estado(altaAdulto, HttpStatusCode.Created);
        var responsableId = (await altaAdulto.Content.ReadFromJsonAsync<RegistrarResponsableRespuesta>())!.Id;
        var responsable = (await cliente.GetFromJsonAsync<ConsultarResponsableRespuesta>(ruta + $"/responsables/{responsableId}"))!;
        comprobar(responsable.Nombre == "Juana" && responsable.Telefono == "12345" && responsable.Documento == "00012345" &&
            responsable.CorreoElectronico is null, "El responsable conserva contacto, documento con ceros iniciales y opcionales nulos.");
        var vinculo = new VincularResponsablePeticion(
            responsableId,
            "Madre",
            true,
            true);
        await Estado(await cliente.PostAsJsonAsync(consulta + "/responsables", vinculo with { TipoVinculo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(consulta + "/responsables", vinculo with { ResponsableId = Guid.NewGuid() }), HttpStatusCode.NotFound);
        await Estado(await cliente.PostAsJsonAsync(consulta + "/responsables", vinculo), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(consulta + "/responsables", vinculo), HttpStatusCode.Conflict);
        aspirante = (await cliente.GetFromJsonAsync<ConsultarAspiranteRespuesta>(consulta))!;
        comprobar(aspirante.Responsables.Single().Responsable.Id == responsableId && aspirante.Responsables.Single().TipoVinculo == "Madre" &&
            aspirante.Responsables.Single().ContactoPrincipal && aspirante.Responsables.Single().EsTutor,
            "El vínculo reconstruye la persona existente, su relación y los indicadores declarados.");
        var hermano = (await (await cliente.PostAsJsonAsync(ruta + "/aspirantes", entrada with { Nombre = "Luis", Dni = "12345678" }))
            .Content.ReadFromJsonAsync<RegistrarAspiranteRespuesta>())!.Id;
        await Estado(await cliente.PostAsJsonAsync(ruta + $"/aspirantes/{hermano}/responsables", vinculo), HttpStatusCode.OK);
        comprobar((await cliente.GetFromJsonAsync<ConsultarAspiranteRespuesta>(ruta + $"/aspirantes/{hermano}"))!.Responsables.Single().Responsable.Id == responsableId,
            "Dos aspirantes reutilizan el mismo responsable por Id, sin copiar sus datos.");
        var busqueda = (await cliente.GetFromJsonAsync<BuscarResponsablesRespuesta>(ruta + "/responsables?documento=00012345"))!;
        comprobar(busqueda.Resultados.Single().Id == responsableId && !busqueda.HayMas, "La búsqueda institucional permite seleccionar un responsable existente.");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/responsables", adulto with { Nombre = "Sin documento", Documento = null }), HttpStatusCode.Created);
        busqueda = (await cliente.GetFromJsonAsync<BuscarResponsablesRespuesta>(ruta + "/responsables?pagina=1&tamanoPagina=1"))!;
        comprobar(busqueda.Resultados.Count == 1 && busqueda.HayMas, "El catálogo de responsables está paginado.");
        await Estado(await cliente.GetAsync(ruta + "/responsables?pagina=0"), HttpStatusCode.BadRequest);
        await Estado(await cliente.GetAsync(ruta + "/responsables?tamanoPagina=101"), HttpStatusCode.BadRequest);
        await Estado(await cliente.GetAsync(ruta + "/responsables?pagina=2147483647&tamanoPagina=100"), HttpStatusCode.BadRequest);
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(ruta + "/personas/historial"))!;
        comprobar(historial.Length == 6 && historial.All(c => c.UsuarioId != Guid.Empty && c.Fecha != default) &&
            historial.Count(c => c.Accion == "Responsable.Vinculacion") == 2,
            "Los registros y vínculos tienen auditoría; los intentos inválidos no se guardan.");
        await Estado(await cliente.GetAsync(otraRuta + $"/responsables/{responsableId}"), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(otraRuta + "/aspirantes", entrada), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        var ajeno = (await (await cliente.PostAsJsonAsync(otraRuta + "/responsables", adulto))
            .Content.ReadFromJsonAsync<RegistrarResponsableRespuesta>())!.Id;
        await Estado(await cliente.GetAsync(ruta + $"/responsables/{ajeno}"), HttpStatusCode.NotFound);
        await Estado(await cliente.GetAsync(otraRuta + $"/aspirantes/{aspiranteId}"), HttpStatusCode.NotFound);
        await Estado(await cliente.PostAsJsonAsync(consulta + "/responsables", vinculo with { ResponsableId = ajeno }), HttpStatusCode.NotFound);
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(consulta), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(ruta + "/responsables?documento=00012345"), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(ruta + "/personas/historial"), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(consulta + "/responsables", vinculo), HttpStatusCode.Forbidden);
        await iniciarSesion("inscripciones@prueba.example");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>();
            var usuario = await contexto.UsuariosPersonal.Include(u => u.Accesos).SingleAsync(u => u.CorreoElectronico == "inscripciones@prueba.example");
            comprobar(usuario.RevocarAcceso(institucion), "La preparación revoca el acceso institucional existente.");
            await contexto.SaveChangesAsync();
        }
        await Estado(await cliente.GetAsync(consulta), HttpStatusCode.Forbidden);
        await aplicacion.AsignarAccesoAsync(
            "inscripciones@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoUsuarios>();
            (await contexto.UsuariosPersonal.SingleAsync(u => u.CorreoElectronico == "inscripciones@prueba.example")).Desactivar();
            await contexto.SaveChangesAsync();
        }
        await Estado(await cliente.GetAsync(consulta), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoInscripciones>();
            comprobar(await contexto.Responsables.CountAsync(r => r.Id == responsableId) == 1 &&
                await contexto.Vinculos.CountAsync(v => v.ResponsableId == responsableId) == 2,
                "La base tiene una persona responsable y dos vínculos para los hermanos.");
            await PruebasPersonasPersistencia.EjecutarAsync(
                alcance.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
                institucion,
                comprobar);
        }
    }
}
