using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasSolicitudes
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
                throw new Exception($"Solicitudes: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Solicitudes HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object entrada)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, entrada);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        async Task<Guid> Aspirante(string ruta, DateOnly nacimiento)
            => (await Alta<RegistrarAspiranteRespuesta>(ruta + "/aspirantes", new RegistrarAspirantePeticion(
                "Aspirante ficticio",
                "Prueba HU07",
                nacimiento))).Id;
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin solicitudes",
            new DatosDomicilio(
                "Calle ficticia",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Id;
        var ruta = $"/api/v1/instituciones/{institucion}";
        var fechas = new CrearPeriodoPeticion(
            2027,
            new DateOnly(
                2026,
                11,
                1),
            new DateOnly(
                2026,
                12,
                1));
        var periodo = (await Alta<CrearPeriodoRespuesta>(ruta + "/periodos", fechas)).Id;
        var periodoRuta = $"/api/v1/periodos/{periodo}";
        var solicitudesRuta = ruta + $"/periodos/{periodo}/solicitudes";
        var aula = (await Alta<RegistrarAulaRespuesta>(ruta + "/aulas", new RegistrarAulaPeticion("Aula prueba"))).Id;
        var manana = (await Alta<RegistrarTurnoRespuesta>(ruta + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var tarde = (await Alta<RegistrarTurnoRespuesta>(ruta + "/turnos", new RegistrarTurnoPeticion("Tarde"))).Id;
        var ofertaEntrada = new RegistrarOfertaPeticion(
            aula,
            manana,
            10,
            true,
            "Acta ficticia",
            "Relevamiento confirmado",
            [new DatosOcupacionInicial(5, 10)],
            "Oferta para solicitudes");
        var oferta = (await Alta<RegistrarOfertaRespuesta>(periodoRuta + "/ofertas", ofertaEntrada)).Id;
        await Alta<RegistrarOfertaRespuesta>(periodoRuta + "/ofertas", ofertaEntrada with { TurnoId = tarde, Ocupaciones = [] });
        var fechaCorte = new DateOnly(
            2027,
            6,
            30);
        var reglas = (await Alta<PublicarVersionRespuesta>(periodoRuta + "/versiones-reglas", new PublicarReglasPeticion(
            fechaCorte,
            "Perfil prototipo",
            "Reglas para prueba"))).Id;
        var versionOferta = (await Alta<PublicarVersionRespuesta>(periodoRuta + "/versiones-oferta", new PublicarOfertaPeticion("Acta", "Publicar"))).Id;
        await Estado(await cliente.PutAsJsonAsync(periodoRuta + "/configuracion", new AsignarConfiguracionPeticion(
            new DatosVersionesConfiguracion(reglas, versionOferta),
            1,
            "Configurar período")), HttpStatusCode.OK);
        var aspirante = await Aspirante(ruta, fechaCorte.AddYears(-5));
        var domicilio = new DatosDomicilioSolicitud(
            " Calle familia ",
            " 123 ",
            "La Plata",
            "Buenos Aires",
            Barrio: "Centro");
        var entrada = new RegistrarSolicitudPeticion(
            aspirante,
            domicilio,
            manana,
            [tarde],
            "Declaración presencial ficticia");
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(periodoRuta + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        await aplicacion.AsignarAccesoAsync(
            "solicitudes@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        await iniciarSesion("solicitudes@prueba.example");
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        foreach (var invalido in new[] { domicilio with { Calle = " " }, domicilio with { Numero = " " }, domicilio with { Localidad = " " }, domicilio with { Provincia = " " } })
            await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { Domicilio = invalido }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { Domicilio = null! }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { TurnoPreferidoId = Guid.NewGuid() }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { TurnosAlternativosAceptados = [manana] }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { TurnosAlternativosAceptados = [tarde, tarde] }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { TurnosAlternativosAceptados = [Guid.NewGuid()] }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada with { AspiranteId = Guid.NewGuid() }), HttpStatusCode.NotFound);
        var alta = await cliente.PostAsJsonAsync(solicitudesRuta, entrada);
        await Estado(alta, HttpStatusCode.Created);
        var registrada = (await alta.Content.ReadFromJsonAsync<RegistrarSolicitudRespuesta>())!;
        comprobar(registrada.Id != Guid.Empty && alta.Headers.Location is not null && registrada.Sala == 5 && registrada.EstadoRecepcion == "Ordinaria",
            "Una solicitud ordinaria recibe Id y sala calculada sin depender de cupos ni de tener DNI.");
        var consultaRuta = ruta + $"/solicitudes/{registrada.Id}";
        var guardada = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(consultaRuta))!;
        comprobar(guardada.AspiranteId == aspirante && guardada.PeriodoId == periodo && guardada.FechaCorteUtilizada == fechaCorte &&
            guardada.FechaNacimientoUtilizada == fechaCorte.AddYears(-5) && guardada.VersionReglasId == reglas && guardada.VersionOfertaId == versionOferta &&
            guardada.RevisionPeriodoUtilizada == 3 && guardada.Domicilio.Calle == "Calle familia" && guardada.Domicilio.Barrio == "Centro" &&
            guardada.TurnosAlternativosAceptados.SequenceEqual([tarde]) && guardada.UsuarioRegistroId != Guid.Empty && guardada.FechaRegistro != default,
            "La consulta reconstruye institución, período, personas, domicilio, cálculo, versiones, turnos y autor.");
        var repetida = await Alta<RegistrarSolicitudRespuesta>(solicitudesRuta, entrada with { Domicilio = domicilio with { Calle = "Otra dirección" }, TurnosAlternativosAceptados = null });
        comprobar(repetida.Id != registrada.Id && (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(ruta + $"/solicitudes/{repetida.Id}"))!.TurnosAlternativosAceptados.Count == 0,
            "Otra recepción obtiene Id propio; omitir alternativas no autoriza turnos adicionales ni fusiona solicitudes.");
        comprobar((await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(consultaRuta))!.Domicilio.Calle == "Calle familia",
            "La dirección de otra solicitud no modifica la anterior.");
        var nacimientoLimite = await Aspirante(ruta, fechaCorte.AddYears(-5).AddDays(1));
        var limite = await Alta<RegistrarSolicitudRespuesta>(solicitudesRuta, entrada with { AspiranteId = nacimientoLimite });
        comprobar(limite.Sala == 4, "Cumplir cinco al día siguiente del corte mantiene sala de cuatro.");
        var excepcional = await Aspirante(ruta, fechaCorte.AddYears(-2));
        var pendienteEdad = await Alta<RegistrarSolicitudRespuesta>(solicitudesRuta, entrada with { AspiranteId = excepcional });
        comprobar(pendienteEdad.Sala is null && pendienteEdad.RequiereRevisionEdad,
            "Una edad excepcional queda para revisión sin asignación automática a otra sala.");
        await iniciarSesion("root@prueba.example");
        await Estado(await cliente.PutAsJsonAsync($"/api/v1/ofertas/{oferta}", new ActualizarOfertaPeticion(
            0,
            false,
            "Borrador posterior",
            "Sin ocupación declarada",
            [],
            1,
            "Cambio futuro")), HttpStatusCode.OK);
        await Alta<RegistrarSolicitudRespuesta>(solicitudesRuta, entrada);
        comprobar(true, "El registro usa la oferta publicada asignada, no el borrador modificado después.");
        await Estado(await cliente.PostAsJsonAsync(periodoRuta + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Cerrar")), HttpStatusCode.OK);
        var manipulada = JsonSerializer.SerializeToNode(entrada, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        manipulada["estadoRecepcion"] = "Ordinaria";
        manipulada["sala"] = 3;
        var fuera = await Alta<RegistrarSolicitudRespuesta>(solicitudesRuta, manipulada);
        var fueraLeida = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(ruta + $"/solicitudes/{fuera.Id}"))!;
        comprobar(fuera.EstadoRecepcion == "FueraDeTerminoPendienteAutorizacion" && fueraLeida.FueraDeTermino && fuera.Sala == 5,
            "Después del cierre se guarda como fuera de término pendiente; el cliente no decide estado ni sala.");
        var lista = (await cliente.GetFromJsonAsync<ListarSolicitudesRespuesta>(solicitudesRuta + "?tamanoPagina=1"))!;
        comprobar(lista.Resultados.Count == 1 && lista.HayMas, "El listado del período está paginado.");
        await Estado(await cliente.GetAsync(solicitudesRuta + "?pagina=0"), HttpStatusCode.BadRequest);
        await Estado(await cliente.GetAsync(solicitudesRuta + "?tamanoPagina=101"), HttpStatusCode.BadRequest);
        var sinConfig = (await Alta<CrearPeriodoRespuesta>(ruta + "/periodos", fechas with { Ciclo = 2028 })).Id;
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/periodos/{sinConfig}/abrir", new CambiarRecepcionPeticion(1, "Apertura del prototipo")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(ruta + $"/periodos/{sinConfig}/solicitudes", entrada), HttpStatusCode.Conflict);
        var nuevasReglas = (await Alta<PublicarVersionRespuesta>(periodoRuta + "/versiones-reglas", new PublicarReglasPeticion(
            fechaCorte.AddDays(1),
            "Nuevo corte",
            "Cambiar para el futuro"))).Id;
        await Estado(await cliente.PutAsJsonAsync(periodoRuta + "/configuracion", new AsignarConfiguracionPeticion(
            new DatosVersionesConfiguracion(nuevasReglas, versionOferta),
            4,
            "Elegir nueva configuración")), HttpStatusCode.OK);
        comprobar((await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(consultaRuta))!.FechaCorteUtilizada == fechaCorte,
            "Cambiar la configuración no recalcula silenciosamente las solicitudes históricas.");
        var otraInstitucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Otra institución HU07",
            new DatosDomicilio(
                "Calle",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Id;
        await Estado(await cliente.GetAsync($"/api/v1/instituciones/{otraInstitucion}/solicitudes/{registrada.Id}"), HttpStatusCode.NotFound);
        await Estado(await cliente.PostAsJsonAsync($"/api/v1/instituciones/{otraInstitucion}/periodos/{periodo}/solicitudes", entrada), HttpStatusCode.NotFound);
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(consultaRuta), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(solicitudesRuta), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(solicitudesRuta, entrada), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(ruta + "/personas/historial"))!;
        comprobar(historial.Count(c => c.Accion == "Solicitud.Registro") == 6 &&
            historial.Any(c => c.EntidadId == fuera.Id && c.Datos.Contains("FueraDeTerminoPendienteAutorizacion")),
            "Cada solicitud y su situación de recepción quedan auditadas sin guardar intentos rechazados.");
        await PruebasCierreRecepcion.EjecutarAsync(
            cliente,
            iniciarSesion,
            new EscenarioCierreRecepcion(
                institucion,
                periodo,
                registrada.Id,
                fuera.Id,
                reglas,
                nuevasReglas,
                versionOferta,
                entrada),
            comprobar);
        using var alcance = aplicacion.Services.CreateScope();
        await PruebasSolicitudesPersistencia.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
            alcance.ServiceProvider.GetRequiredService<DbContextOptions<Jardin.Configuracion.Infrastructure.Persistencia.ContextoConfiguracion>>(),
            institucion,
            aspirante,
            comprobar);
    }
}
