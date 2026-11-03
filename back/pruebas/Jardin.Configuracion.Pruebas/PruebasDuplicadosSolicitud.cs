using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Domain.Servicios;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasDuplicadosSolicitud
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
                throw new Exception($"Duplicados: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Duplicados HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin duplicados",
            new DatosDomicilio(
                "Calle ficticia",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Id;
        var raiz = $"/api/v1/instituciones/{institucion}";
        var aula = (await Alta<RegistrarAulaRespuesta>(raiz + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var turno = (await Alta<RegistrarTurnoRespuesta>(raiz + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        async Task<Guid> CrearPeriodo(int ciclo)
        {
            var id = (await Alta<CrearPeriodoRespuesta>(raiz + "/periodos", new CrearPeriodoPeticion(
                ciclo,
                new DateOnly(
                2026,
                11,
                1),
                new DateOnly(
                2026,
                12,
                1)))).Id;
            var ruta = $"/api/v1/periodos/{id}";
            await Alta<RegistrarOfertaRespuesta>(ruta + "/ofertas", new RegistrarOfertaPeticion(
                aula,
                turno,
                20,
                true,
                "Acta ficticia",
                "Relevamiento confirmado",
                [],
                "Preparar prueba"));
            var reglas = (await Alta<PublicarVersionRespuesta>(ruta + "/versiones-reglas", new PublicarReglasPeticion(
                new DateOnly(
                ciclo,
                6,
                30),
                "Perfil prototipo",
                "Publicar"))).Id;
            var oferta = (await Alta<PublicarVersionRespuesta>(ruta + "/versiones-oferta", new PublicarOfertaPeticion("Acta", "Publicar"))).Id;
            await Estado(await cliente.PutAsJsonAsync(ruta + "/configuracion", new AsignarConfiguracionPeticion(
                new DatosVersionesConfiguracion(reglas, oferta),
                1,
                "Asignar")), HttpStatusCode.OK);
            await Estado(await cliente.PostAsJsonAsync(ruta + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
            return id;
        }
        var periodo = await CrearPeriodo(2027);
        var segundoPeriodo = await CrearPeriodo(2028);
        var rutaPeriodo = raiz + $"/periodos/{periodo}";
        async Task<Guid> Persona(
            string nombre,
            string apellido,
            string? dni,
            DateOnly nacimiento)
            => (await Alta<RegistrarAspiranteRespuesta>(raiz + "/aspirantes", new RegistrarAspirantePeticion(
                nombre,
                apellido,
                nacimiento,
                dni))).Id;
        async Task<RegistrarSolicitudRespuesta> Solicitud(Guid persona, Guid periodoId)
            => await Alta<RegistrarSolicitudRespuesta>(raiz + $"/periodos/{periodoId}/solicitudes", new RegistrarSolicitudPeticion(
                persona,
                new DatosDomicilioSolicitud(
                    "Calle familia",
                    "10",
                    "La Plata",
                    "Buenos Aires"),
                turno));
        var nacimiento = new DateOnly(
                2022,
                6,
                30);
        var a = await Persona(
            "Ana",
            "Ejemplo",
            "00012345",
            nacimiento);
        var primera = await Solicitud(a, periodo);
        var repetida = await Solicitud(a, periodo);
        comprobar(primera.AlertasPosibleDuplicado == 0 && repetida.AlertasPosibleDuplicado == 1 && primera.Id != repetida.Id,
            "Registrar otra solicitud genera la alerta automáticamente, pero conserva el registro con Id propio.");
        var b = await Persona(
            "Otro nombre",
            "Otro apellido",
            "00012345",
            new DateOnly(
                2023,
                7,
                1));
        comprobar((await Solicitud(b, periodo)).AlertasPosibleDuplicado == 2,
            "Un DNI completo repetido genera alertas aunque difieran los demás datos.");
        var c = await Persona(
            " ana ",
            " ejemplo ",
            null,
            nacimiento);
        comprobar((await Solicitud(c, periodo)).AlertasPosibleDuplicado == 2,
            "Nombre, apellido y nacimiento coincidentes generan alerta sin exigir DNI.");
        var d = await Persona(
            "Ana",
            "Otro",
            null,
            nacimiento);
        comprobar((await Solicitud(d, periodo)).AlertasPosibleDuplicado == 0, "Compartir solo parte del nombre no genera alerta.");
        var e = await Persona(
            "Sin",
            "Documento",
            null,
            new DateOnly(
                2024,
                2,
                1));
        comprobar((await Solicitud(e, periodo)).AlertasPosibleDuplicado == 0,
            "Dos DNI ausentes no se comparan como si fueran el mismo documento.");
        comprobar((await Solicitud(a, segundoPeriodo)).AlertasPosibleDuplicado == 0,
            "La misma persona en otro período no se considera una solicitud duplicada de este proceso.");
        await aplicacion.AsignarAccesoAsync(
            "duplicados@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        await iniciarSesion("duplicados@prueba.example");
        async Task<ListarDuplicadosRespuesta> Lista(string sufijo = "")
            => (await cliente.GetFromJsonAsync<ListarDuplicadosRespuesta>(rutaPeriodo + "/duplicados" + sufijo))!;
        var lista = await Lista();
        comprobar(lista.Resultados.Count == 5 && lista.Resultados.All(x => x.Estado == "PendienteRevision" && x.Revision == 1),
            "Cada pareja tiene una sola alerta con motivos y estado inicial pendiente.");
        bool EsPareja(
            ConsultarDuplicadoRespuesta alerta,
            Guid x,
            Guid y)
            => (alerta.SolicitudAId == x && alerta.SolicitudBId == y) || (alerta.SolicitudAId == y && alerta.SolicitudBId == x);
        var mismaPersona = lista.Resultados.Single(x => EsPareja(
            x,
            primera.Id,
            repetida.Id));
        var casoDni = lista.Resultados.First(x => x.Criterios.Count == 1 && x.Criterios.Contains("DniCompleto"));
        comprobar(mismaPersona.Criterios.Count == 3 && mismaPersona.PerfilDeteccion == DetectorPosiblesDuplicados.Perfil &&
            mismaPersona.UsuarioDeteccionId != Guid.Empty && mismaPersona.FechaDeteccion != default,
            "La alerta conserva criterios, perfil y autor/fecha de detección.");
        var decision = new ResolverDuplicadoPeticion(
            "DuplicacionConfirmada",
            1,
            "Se cotejaron las dos presentaciones");
        var resolver = raiz + $"/duplicados/{mismaPersona.Id}/resolver";
        await Estado(await cliente.PostAsJsonAsync(resolver, decision), HttpStatusCode.Forbidden);
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(rutaPeriodo + "/duplicados"), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        var deteccion = await cliente.PostAsync(rutaPeriodo + "/duplicados/detectar", null);
        await Estado(deteccion, HttpStatusCode.OK);
        var resultado = (await deteccion.Content.ReadFromJsonAsync<DetectarDuplicadosRespuesta>())!;
        comprobar(resultado.Nuevas == 0 && resultado.Actualizadas == 0, "Repetir detección sin cambios es idempotente.");
        comprobar((await Lista("?tamanoPagina=1")).HayMas, "Las alertas se consultan con paginación.");
        await Estado(await cliente.GetAsync(rutaPeriodo + "/duplicados?pagina=0"), HttpStatusCode.BadRequest);
        await Estado(await cliente.GetAsync(rutaPeriodo + "/duplicados?estado=1"), HttpStatusCode.BadRequest);
        await aplicacion.AsignarAccesoAsync(
            "directivo@prueba.example",
            institucion,
            RolesPersonal.Directivo);
        await iniciarSesion("directivo@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(resolver, decision with { Motivo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(resolver, decision with { Decision = "PendienteRevision" }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(resolver, decision), HttpStatusCode.OK);
        var resuelta = (await cliente.GetFromJsonAsync<ConsultarDuplicadoRespuesta>(raiz + $"/duplicados/{mismaPersona.Id}"))!;
        comprobar(resuelta.Estado == "DuplicacionConfirmada" && resuelta.Revision == 2 && resuelta.UsuarioResolucionId is not null &&
            resuelta.FechaResolucion is not null && resuelta.MotivoResolucion == decision.Motivo,
            "Directivo conserva la resolución y su motivo, usuario y fecha.");
        await Estado(await cliente.PostAsJsonAsync(resolver, decision), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(raiz + $"/duplicados/{casoDni.Id}/resolver", decision with { Decision = "PersonasDiferentes", Motivo = "La revisión determina personas distintas" }), HttpStatusCode.OK);
        await Estado(await cliente.PostAsync(rutaPeriodo + "/duplicados/detectar", null), HttpStatusCode.OK);
        comprobar((await cliente.GetFromJsonAsync<ConsultarDuplicadoRespuesta>(raiz + $"/duplicados/{mismaPersona.Id}"))!.Estado == "DuplicacionConfirmada" &&
            (await cliente.GetFromJsonAsync<ConsultarDuplicadoRespuesta>(raiz + $"/duplicados/{casoDni.Id}"))!.Estado == "PersonasDiferentes",
            "Una detección repetida no reabre ni reemplaza decisiones administrativas sin datos nuevos.");
        comprobar((await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(raiz + $"/solicitudes/{primera.Id}"))!.EstadoRecepcion == "Ordinaria" &&
            (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(raiz + $"/solicitudes/{repetida.Id}"))!.EstadoRecepcion == "Ordinaria",
            "Confirmar o descartar una alerta no elimina, fusiona ni excluye solicitudes.");
        // Simula una futura corrección de identidad, aún no expuesta como operación de HU-09.
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoInscripciones>();
            var persona = await contexto.Aspirantes.SingleAsync(x => x.Id == b);
            persona.CorregirDatos(
                persona.Nombre,
                persona.Apellido,
                "99887766",
                persona.FechaNacimiento,
                persona.TipoDocumento,
                persona.Sexo);
            await contexto.SaveChangesAsync();
        }
        await Estado(await cliente.PostAsJsonAsync(raiz + $"/duplicados/{casoDni.Id}/resolver", decision with { Revision = 2 }), HttpStatusCode.Conflict);
        var nuevaDeteccion = await cliente.PostAsync(rutaPeriodo + "/duplicados/detectar", null);
        await Estado(nuevaDeteccion, HttpStatusCode.OK);
        var actualizada = (await nuevaDeteccion.Content.ReadFromJsonAsync<DetectarDuplicadosRespuesta>())!;
        comprobar(actualizada.Nuevas == 0 && actualizada.Actualizadas == 2, "Datos de identidad nuevos actualizan las alertas relacionadas, conservando las demás.");
        var pendiente = (await cliente.GetFromJsonAsync<ConsultarDuplicadoRespuesta>(raiz + $"/duplicados/{casoDni.Id}"))!;
        comprobar(pendiente.Estado == "PendienteRevision" && pendiente.Revision == 3 && pendiente.Criterios.Count == 0 && pendiente.MotivoResolucion is null,
            "Si ya no hay coincidencia, la alerta histórica sigue disponible para revisión humana, sin descarte automático.");
        await Estado(await cliente.PostAsJsonAsync(raiz + $"/duplicados/{casoDni.Id}/resolver", decision with { Decision = "PersonasDiferentes", Revision = 3, Motivo = "Revisados los datos corregidos" }), HttpStatusCode.OK);
        comprobar((await Lista("?estado=DuplicacionConfirmada")).Resultados.Count == 1, "El listado filtra el estado de revisión solicitado.");
        await iniciarSesion("root@prueba.example");
        var otraInstitucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Otro jardín HU10",
            new DatosDomicilio(
                "Calle",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Id;
        await Estado(await cliente.GetAsync($"/api/v1/instituciones/{otraInstitucion}/duplicados/{mismaPersona.Id}"), HttpStatusCode.NotFound);
        await Estado(await cliente.PostAsync($"/api/v1/instituciones/{otraInstitucion}/periodos/{periodo}/duplicados/detectar", null), HttpStatusCode.NotFound);
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(rutaPeriodo + "/duplicados"), HttpStatusCode.Forbidden);
        await Estado(await cliente.GetAsync(raiz + $"/duplicados/{mismaPersona.Id}"), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsync(rutaPeriodo + "/duplicados/detectar", null), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(resolver, decision), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(raiz + "/personas/historial"))!;
        comprobar(historial.Count(h => h.Accion.StartsWith("Duplicado.")) == 10 &&
            historial.Any(h => h.EntidadId == casoDni.Id && h.Accion == "Duplicado.DeteccionActualizada" && h.Datos.Contains("PersonasDiferentes") && h.Datos.Contains("PendienteRevision")),
            "El historial conserva detección, resolución anterior, cambio de datos y nueva resolución.");
        using var final = aplicacion.Services.CreateScope();
        await PruebasDuplicadosPersistencia.EjecutarAsync(
            final.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
            institucion,
            periodo,
            primera.Id,
            mismaPersona.Id,
            comprobar);
        PruebasReglasDuplicados.Ejecutar(comprobar);
    }
}

