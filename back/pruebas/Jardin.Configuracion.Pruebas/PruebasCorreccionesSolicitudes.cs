using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Application.Seguridad;
using Jardin.Inscripciones.Application.Servicios;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasCorreccionesSolicitudes
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
                throw new Exception($"Correcciones: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Correcciones HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object entrada)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, entrada);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin correcciones",
            new DatosDomicilio(
                "Calle jardín",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Id;
        var raiz = $"/api/v1/instituciones/{institucion}";
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
        var p = $"/api/v1/periodos/{periodo}";
        var aula = (await Alta<RegistrarAulaRespuesta>(raiz + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var manana = (await Alta<RegistrarTurnoRespuesta>(raiz + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var tarde = (await Alta<RegistrarTurnoRespuesta>(raiz + "/turnos", new RegistrarTurnoPeticion("Tarde"))).Id;
        var noPublicado = (await Alta<RegistrarTurnoRespuesta>(raiz + "/turnos", new RegistrarTurnoPeticion("No publicado"))).Id;
        var ofertaEntrada = new RegistrarOfertaPeticion(
            aula,
            manana,
            20,
            true,
            "Acta ficticia",
            "Relevamiento confirmado",
            [],
            "Preparar prueba");
        await Alta<RegistrarOfertaRespuesta>(p + "/ofertas", ofertaEntrada);
        var ofertaTarde = (await Alta<RegistrarOfertaRespuesta>(p + "/ofertas", ofertaEntrada with { TurnoId = tarde })).Id;
        var reglas = (await Alta<PublicarVersionRespuesta>(p + "/versiones-reglas", new PublicarReglasPeticion(
            new DateOnly(
                2027,
                6,
                30),
            "Perfil",
            "Publicar"))).Id;
        var oferta = (await Alta<PublicarVersionRespuesta>(p + "/versiones-oferta", new PublicarOfertaPeticion("Acta", "Publicar"))).Id;
        await Estado(await cliente.PutAsJsonAsync(p + "/configuracion", new AsignarConfiguracionPeticion(
            new DatosVersionesConfiguracion(reglas, oferta),
            1,
            "Asignar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(p + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        var aspirante = (await Alta<RegistrarAspiranteRespuesta>(raiz + "/aspirantes", new RegistrarAspirantePeticion(
            "Ana",
            "Ejemplo",
            new DateOnly(
                2022,
                6,
                30),
            "00012345"))).Id;
        var sinDni = (await Alta<RegistrarAspiranteRespuesta>(raiz + "/aspirantes", new RegistrarAspirantePeticion(
            "José",
            "Otro",
            new DateOnly(
                2023,
                6,
                30)))).Id;
        var domicilio = new DatosDomicilioSolicitud(
            "Dirección original",
            "10",
            "La Plata",
            "Buenos Aires");
        var solicitudEntrada = new RegistrarSolicitudPeticion(
            aspirante,
            domicilio,
            manana,
            [tarde]);
        var solicitud = (await Alta<RegistrarSolicitudRespuesta>(raiz + $"/periodos/{periodo}/solicitudes", solicitudEntrada)).Id;
        var otra = (await Alta<RegistrarSolicitudRespuesta>(raiz + $"/periodos/{periodo}/solicitudes", solicitudEntrada with { Domicilio = domicilio with { Calle = "Otra solicitud" } })).Id;
        await Alta<RegistrarSolicitudRespuesta>(raiz + $"/periodos/{periodo}/solicitudes", solicitudEntrada with { AspiranteId = sinDni });
        var ruta = raiz + $"/solicitudes/{solicitud}";
        var buscar = raiz + "/solicitudes/buscar";
        var referenciaMedicion = Guid.NewGuid();
        var referenciaIngreso = Guid.NewGuid();
        var referenciaDistribucion = Guid.NewGuid();
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoInscripciones>();
            // Referencias sintéticas: prueba del índice, no procesos reales de medición o admisión.
            var usos = new ServicioUsoDatosSolicitud(
                new RepositorioUsoDatosSolicitud(contexto),
                new RepositorioSolicitudes(contexto),
                new AccesoIndicePrueba());
            foreach (var (id, tipo) in new[] { (referenciaMedicion, "MedicionDistancia"), (referenciaIngreso, "PropuestaIngreso"), (referenciaDistribucion, "PropuestaDistribucion") })
                await usos.RegistrarUsoAsync(
                    institucion,
                    solicitud,
                    new DatosUsoSolicitud(
                        id,
                        tipo,
                        1,
                        1));
            await usos.RegistrarUsoAsync(
                institucion,
                solicitud,
                new DatosUsoSolicitud(
                    referenciaMedicion,
                    "MedicionDistancia",
                    1,
                    1));
            comprobar(await contexto.UsosDatosSolicitud.CountAsync(u => u.SolicitudId == solicitud) == 3,
                "Repetir una referencia no duplica el índice de dependencias.");
            try
            {
                await usos.RegistrarUsoAsync(
                    institucion,
                    solicitud,
                    new DatosUsoSolicitud(
                        Guid.NewGuid(),
                        "MedicionDistancia",
                        2,
                        2));
                throw new Exception("Se aceptó una revisión futura.");
            }
            catch (ArgumentException) { comprobar(true, "El índice rechaza revisiones futuras."); }
        }
        comprobar((await cliente.GetFromJsonAsync<ConsultarImpactoSolicitudRespuesta>(ruta + "/impacto"))!.UsosDesactualizados.Count == 0,
            "Las referencias con datos actuales no se señalan como desactualizadas.");
        await Estado(await cliente.PutAsJsonAsync($"/api/v1/ofertas/{ofertaTarde}", new ActualizarOfertaPeticion(
            20,
            false,
            "Oferta futura",
            "Relevamiento",
            [],
            1,
            "Cambio futuro")), HttpStatusCode.OK);
        var futura = (await Alta<PublicarVersionRespuesta>(p + "/versiones-oferta", new PublicarOfertaPeticion("Acta futura", "Publicar"))).Id;
        await Estado(await cliente.PutAsJsonAsync(p + "/configuracion", new AsignarConfiguracionPeticion(
            new DatosVersionesConfiguracion(reglas, futura),
            3,
            "Elegir oferta futura")), HttpStatusCode.OK);
        await aplicacion.AsignarAccesoAsync(
            "correcciones@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        await iniciarSesion("correcciones@prueba.example");
        async Task<BuscarSolicitudesRespuesta> Buscar(BuscarSolicitudesPeticion filtro)
        {
            var respuesta = await cliente.PostAsJsonAsync(buscar, filtro);
            await Estado(respuesta, HttpStatusCode.OK);
            return (await respuesta.Content.ReadFromJsonAsync<BuscarSolicitudesRespuesta>())!;
        }
        comprobar((await Buscar(new BuscarSolicitudesPeticion(SolicitudId: solicitud))).Resultados.Single().Id == solicitud,
            "La búsqueda por identificador devuelve la solicitud del contexto autorizado.");
        comprobar((await Buscar(new BuscarSolicitudesPeticion(Nombre: "ana", Apellido: "JEMP", Dni: "00012345"))).Resultados.Count == 2,
            "Nombre/apellido permiten coincidencias parciales sin distinguir mayúsculas y el DNI mantiene sus ceros iniciales.");
        comprobar((await Buscar(new BuscarSolicitudesPeticion(Nombre: "José"))).Resultados.Single().Dni is null &&
            (await Buscar(new BuscarSolicitudesPeticion(Nombre: "José", Dni: "00012345"))).Resultados.Count == 0,
            "Se encuentran aspirantes sin DNI y los filtros combinados se aplican conjuntamente.");
        var pagina = await Buscar(new BuscarSolicitudesPeticion(TamanoPagina: 1));
        comprobar(pagina.Resultados.Count == 1 && pagina.HayMas, "La búsqueda está paginada.");
        await Estado(await cliente.PostAsJsonAsync(buscar, new BuscarSolicitudesPeticion(Pagina: 0)), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(buscar, new BuscarSolicitudesPeticion(TamanoPagina: 101)), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(buscar, new BuscarSolicitudesPeticion(SolicitudId: Guid.Empty)), HttpStatusCode.BadRequest);
        var correccion = new CorregirSolicitudPeticion(
            domicilio,
            tarde,
            [manana],
            1,
            "Corregir preferencia declarada");
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PutAsJsonAsync(ruta, correccion), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        await Estado(await cliente.PutAsJsonAsync(ruta, correccion with { Motivo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PutAsJsonAsync(ruta, correccion with { Domicilio = domicilio with { Calle = " " } }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PutAsJsonAsync(ruta, correccion with { TurnoPreferidoId = noPublicado }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PutAsJsonAsync(ruta, correccion with { TurnosAlternativosAceptados = [tarde] }), HttpStatusCode.BadRequest);
        var cambio = await cliente.PutAsJsonAsync(ruta, correccion);
        await Estado(cambio, HttpStatusCode.OK);
        var cambioLeido = (await cambio.Content.ReadFromJsonAsync<CorregirSolicitudRespuesta>())!;
        comprobar(cambioLeido.RevisionSolicitud == 2 && cambioLeido.RevisionDomicilio == 1 && !cambioLeido.CambioDomicilio,
            "Cambiar preferencias incrementa la revisión general, sin invalidar por sí mismo la medición del domicilio.");
        var impacto = (await cliente.GetFromJsonAsync<ConsultarImpactoSolicitudRespuesta>(ruta + "/impacto"))!;
        comprobar(impacto.UsosDesactualizados.Count == 2 && impacto.UsosDesactualizados.All(u => u.Tipo != "MedicionDistancia"),
            "El índice distingue impacto de preferencias en propuestas de una medición de dirección sin cambios.");
        await Estado(await cliente.PutAsJsonAsync(ruta, correccion), HttpStatusCode.Conflict);
        var corregirDireccion = correccion with { Domicilio = domicilio with { Calle = "Dirección corregida" }, TurnoPreferidoId = manana, TurnosAlternativosAceptados = [tarde], RevisionSolicitud = 2, Motivo = "Corregir domicilio" };
        await Estado(await cliente.PutAsJsonAsync(ruta, corregirDireccion), HttpStatusCode.OK);
        var leida = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(ruta))!;
        comprobar(leida.RevisionDatos == 3 && leida.RevisionDomicilio == 2 && leida.Domicilio.Calle == "Dirección corregida" &&
            leida.TurnosAlternativosAceptados.SequenceEqual([tarde]) && leida.VersionOfertaId == oferta && leida.VersionReglasId == reglas &&
            leida.FechaCorteUtilizada == new DateOnly(
                2027,
                6,
                30) && leida.EstadoRecepcion == "Ordinaria",
            "Se reconstruyen dirección y alternativas corregidas sin cambiar versiones, cálculo o recepción históricos.");
        impacto = (await cliente.GetFromJsonAsync<ConsultarImpactoSolicitudRespuesta>(ruta + "/impacto"))!;
        comprobar(impacto.UsosDesactualizados.Count == 3 && impacto.UsosDesactualizados.Any(u => u.ReferenciaId == referenciaMedicion),
            "Cambiar el domicilio identifica también la medición registrada que usó la dirección anterior.");
        var consentimiento = corregirDireccion with { RevisionSolicitud = 3, Motivo = "Corregir referencia", ReferenciaConsentimiento = "Constancia corregida" };
        await Estado(await cliente.PutAsJsonAsync(ruta, consentimiento), HttpStatusCode.OK);
        await Estado(await cliente.PutAsJsonAsync(ruta, consentimiento with { RevisionSolicitud = 4 }), HttpStatusCode.Conflict);
        var otraLeida = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(raiz + $"/solicitudes/{otra}"))!;
        var persona = (await cliente.GetFromJsonAsync<ConsultarAspiranteRespuesta>(raiz + $"/aspirantes/{aspirante}"))!;
        comprobar(otraLeida.RevisionDatos == 1 && otraLeida.Domicilio.Calle == "Otra solicitud" && persona.Nombre == "Ana" && persona.Dni == "00012345",
            "La corrección no modifica otra solicitud ni al aspirante compartido.");
        var condicion = await Alta<RegistrarCondicionRespuesta>(ruta + "/condiciones", new RegistrarCondicionPeticion(
            "Hermanos",
            "Declara un hermano regular",
            4,
            "Declaración inicial"));
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", new RevisarCondicionPeticion(
            "Verificada",
            5,
            "Verificación inicial",
            "Referencia anterior")), HttpStatusCode.OK);
        var corregirCondicion = new CorregirCondicionPeticion(
            "Hermanos",
            "No corresponde el vínculo declarado inicialmente",
            6,
            "Corregir declaración familiar");
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}/declaracion", corregirCondicion), HttpStatusCode.OK);
        var declaracion = (await cliente.GetFromJsonAsync<ConsultarAntecedentesRespuesta>(ruta + "/antecedentes"))!.Condiciones.Single();
        comprobar(declaracion.ValorDeclarado == corregirCondicion.ValorDeclarado && declaracion.EstadoVerificacion == "PendienteRevision" &&
            declaracion.UsuarioRevisionId is null && declaracion.FechaRevision is null && declaracion.EvidenciaReferencia is null,
            "Corregir una declaración no conserva una verificación hecha sobre otro contenido.");
        await iniciarSesion("root@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(p + "/cerrar-recepcion", new CambiarRecepcionPeticion(4, "Cerrar")), HttpStatusCode.OK);
        await iniciarSesion("correcciones@prueba.example");
        await Estado(await cliente.PutAsJsonAsync(ruta, consentimiento with { RevisionSolicitud = 7, ReferenciaConsentimiento = "Después del cierre" }), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}/declaracion", corregirCondicion with { RevisionSolicitud = 7, ValorDeclarado = "Otro dato" }), HttpStatusCode.Forbidden);
        await aplicacion.AsignarAccesoAsync(
            "directivo@prueba.example",
            institucion,
            RolesPersonal.Directivo);
        await iniciarSesion("directivo@prueba.example");
        await Estado(await cliente.PutAsJsonAsync(ruta, consentimiento with { RevisionSolicitud = 7, ReferenciaConsentimiento = "Después del cierre", Motivo = "Corrección autorizada tras cierre" }), HttpStatusCode.OK);
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(raiz + "/personas/historial"))!;
        comprobar(historial.Count(c => c.EntidadId == solicitud && c.Accion == "Solicitud.Correccion") == 4 &&
            historial.Any(c => c.Accion == "Condicion.Correccion" && c.Datos.Contains("Verificada") && c.Datos.Contains("PendienteRevision")),
            "El historial conserva las correcciones, sus valores anteriores/nuevos y el reinicio de la verificación.");
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(buscar, new BuscarSolicitudesPeticion(Dni: "00012345")), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(ruta, consentimiento), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(ruta + "/impacto"), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        using var final = aplicacion.Services.CreateScope();
        await PruebasCorreccionesPersistencia.EjecutarAsync(
            final.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
            institucion,
            solicitud,
            comprobar);
    }

    private sealed class AccesoIndicePrueba : IAccesoInstitucionesInscripciones
    {
        // Implementa IAccesoInstitucionesInscripciones.AutorizarAsync para el consumidor sintético del índice.
        public Task<Guid> AutorizarAsync(
            Guid institucionId,
            bool escritura,
            CancellationToken tokenCancelacion = default) => Task.FromResult(Guid.NewGuid());
        // Implementa IAccesoInstitucionesInscripciones.AutorizarOperacionAsync para el consumidor sintético del índice.
        public Task<Guid> AutorizarOperacionAsync(
            Guid institucionId,
            OperacionInscripciones operacion,
            CancellationToken tokenCancelacion = default) => Task.FromResult(Guid.NewGuid());
    }
}
