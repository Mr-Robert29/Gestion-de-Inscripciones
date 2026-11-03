using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Jardin.Usuarios.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasAntecedentesSolicitud
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
                throw new Exception($"Antecedentes: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Antecedentes HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object entrada)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, entrada);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        var institucion = (await Alta<RegistrarInstitucionRespuesta>("/api/v1/instituciones", new RegistrarInstitucionPeticion(
            "Jardin antecedentes",
            new DatosDomicilio(
                "Calle ficticia",
                "1",
                "La Plata",
                "Buenos Aires"),
            "12345"))).Id;
        var rutaInstitucion = $"/api/v1/instituciones/{institucion}";
        var periodo = (await Alta<CrearPeriodoRespuesta>(rutaInstitucion + "/periodos", new CrearPeriodoPeticion(
            2027,
            new DateOnly(
                2026,
                11,
                1),
            new DateOnly(
                2026,
                12,
                1)))).Id;
        var rutaPeriodo = $"/api/v1/periodos/{periodo}";
        var aula = (await Alta<RegistrarAulaRespuesta>(rutaInstitucion + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var turno = (await Alta<RegistrarTurnoRespuesta>(rutaInstitucion + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas", new RegistrarOfertaPeticion(
            aula,
            turno,
            20,
            true,
            "Acta ficticia",
            "Relevamiento",
            [],
            "Preparar prueba"));
        var reglas = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-reglas", new PublicarReglasPeticion(
            new DateOnly(
                2027,
                6,
                30),
            "Perfil prototipo",
            "Publicar reglas"))).Id;
        var oferta = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-oferta", new PublicarOfertaPeticion("Acta", "Publicar oferta"))).Id;
        await Estado(await cliente.PutAsJsonAsync(rutaPeriodo + "/configuracion", new AsignarConfiguracionPeticion(
            new DatosVersionesConfiguracion(reglas, oferta),
            1,
            "Asignar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        var aspirante = (await Alta<RegistrarAspiranteRespuesta>(rutaInstitucion + "/aspirantes", new RegistrarAspirantePeticion(
            "Aspirante",
            "Ficticio",
            new DateOnly(
                2022,
                6,
                30)))).Id;
        var peticionSolicitud = new RegistrarSolicitudPeticion(
            aspirante,
            new DatosDomicilioSolicitud(
                "Calle familia",
                "10",
                "La Plata",
                "Buenos Aires"),
            turno);
        var solicitud = (await Alta<RegistrarSolicitudRespuesta>(rutaInstitucion + $"/periodos/{periodo}/solicitudes", peticionSolicitud)).Id;
        var ruta = rutaInstitucion + $"/solicitudes/{solicitud}";
        async Task<ConsultarAntecedentesRespuesta> Consultar() => (await cliente.GetFromJsonAsync<ConsultarAntecedentesRespuesta>(ruta + "/antecedentes"))!;
        var vacia = await Consultar();
        comprobar(vacia.RevisionSolicitud == 1 && vacia.Documentos.Count == 0 && vacia.Condiciones.Count == 0,
            "Una solicitud sin antecedentes se conserva; no se genera un catálogo obligatorio inventado.");
        var documentoEntrada = new RegistrarDocumentoPeticion(
            " Partida de nacimiento ",
            "Pendiente",
            1,
            "La familia informa que falta presentarla");
        var token = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = token;
        await aplicacion.AsignarAccesoAsync(
            "antecedentes@prueba.example",
            institucion,
            RolesPersonal.Administrativo);
        await iniciarSesion("antecedentes@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada with { Estado = "Completa" }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada with { Estado = "1" }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada with { Tipo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada with { Motivo = " " }), HttpStatusCode.BadRequest);
        var documento = await Alta<RegistrarDocumentoRespuesta>(ruta + "/documentos", documentoEntrada);
        var despuesDocumento = await Consultar();
        comprobar(documento.RevisionSolicitud == 2 && despuesDocumento.Documentos.Single().Estado == "Pendiente" &&
            despuesDocumento.Documentos.Single().Tipo == "Partida de nacimiento" && despuesDocumento.Documentos.Single().Referencia is null &&
            despuesDocumento.Documentos.Single().UsuarioRevisionId is null && despuesDocumento.Documentos.Single().UsuarioRegistroId != Guid.Empty,
            "Se registra la documentación faltante sin archivo ni referencia obligatorios, conservando autor y solicitud.");
        var condicionEntrada = new RegistrarCondicionPeticion(
            "Hermanos o vínculos convivientes",
            "La familia declara un hermano regular en el establecimiento",
            2,
            "Registrar declaración familiar");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/condiciones", condicionEntrada with { Tipo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/condiciones", condicionEntrada with { ValorDeclarado = " " }), HttpStatusCode.BadRequest);
        var manipulada = JsonSerializer.SerializeToNode(condicionEntrada, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        manipulada["estadoVerificacion"] = "Verificada";
        var condicion = await Alta<RegistrarCondicionRespuesta>(ruta + "/condiciones", manipulada);
        comprobar(condicion.RevisionSolicitud == 3 && (await Consultar()).Condiciones.Single().EstadoVerificacion == "PendienteRevision",
            "Una declaración nueva no se autoverifica aunque el cliente envíe ese campo extra.");
        var revisionCondicion = new RevisarCondicionPeticion(
            "Verificada",
            3,
            "Se cotejó el antecedente",
            "Legajo ficticio 123");
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion with { Estado = "Admitido" }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{Guid.NewGuid()}", revisionCondicion), HttpStatusCode.NotFound);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion), HttpStatusCode.OK);
        var verificada = (await Consultar()).Condiciones.Single();
        comprobar(verificada.EstadoVerificacion == "Verificada" && verificada.ValorDeclarado == condicionEntrada.ValorDeclarado &&
            verificada.EvidenciaReferencia == "Legajo ficticio 123" && verificada.UsuarioRevisionId != null && verificada.FechaRevision != null,
            "La revisión conserva el valor declarado y registra quién verificó, cuándo y con qué referencia.");
        var revisionDocumento = new RevisarDocumentoPeticion(
            "Presentada",
            4,
            "Se recibió el documento presencialmente");
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/documentos/{documento.Id}", revisionDocumento), HttpStatusCode.OK);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/documentos/{documento.Id}", revisionDocumento), HttpStatusCode.Conflict);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/documentos/{documento.Id}", revisionDocumento with { Estado = "Observada", RevisionSolicitud = 5, Observacion = "Revisar legibilidad" }), HttpStatusCode.OK);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion with { Estado = "Observada", RevisionSolicitud = 6, Observacion = "Necesita aclaración" }), HttpStatusCode.OK);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion with { Estado = "PendienteRevision", RevisionSolicitud = 7 }), HttpStatusCode.OK);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion with { RevisionSolicitud = 8 }), HttpStatusCode.OK);
        var revision = await Consultar();
        comprobar(revision.RevisionSolicitud == 9 && revision.Documentos.Single().Estado == "Observada" &&
            (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(ruta))!.RevisionDatos == 9,
            "Los cambios de antecedentes actualizan la revisión general y muestran la documentación por resolver.");
        var historial = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(rutaInstitucion + "/personas/historial"))!;
        comprobar(historial.Count(c => c.EntidadId == solicitud && (c.Accion.StartsWith("Documento.") || c.Accion.StartsWith("Condicion."))) == 8 &&
            historial.Any(c => c.Accion == "Documento.Revision" && c.Datos.Contains("Pendiente") && c.Datos.Contains("Presentada") && JsonSerializer.Deserialize<JsonElement>(c.Datos).GetProperty("Motivo").GetString() == revisionDocumento.Motivo),
            "Cada cambio conserva antes/después, motivo y revisiones; los rechazados no agregan auditoría.");
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada with { RevisionSolicitud = 1 }), HttpStatusCode.Conflict);
        await iniciarSesion("root@prueba.example");
        var otraSolicitud = (await Alta<RegistrarSolicitudRespuesta>(rutaInstitucion + $"/periodos/{periodo}/solicitudes", peticionSolicitud)).Id;
        await Estado(await cliente.PutAsJsonAsync(rutaInstitucion + $"/solicitudes/{otraSolicitud}/documentos/{documento.Id}", revisionDocumento with { RevisionSolicitud = 1 }), HttpStatusCode.NotFound);
        await Estado(await cliente.GetAsync($"/api/v1/instituciones/{Guid.NewGuid()}/solicitudes/{solicitud}/antecedentes"), HttpStatusCode.NotFound);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Cerrar recepción")), HttpStatusCode.OK);
        await iniciarSesion("antecedentes@prueba.example");
        await Alta<RegistrarDocumentoRespuesta>(ruta + "/documentos", documentoEntrada with { Tipo = "DNI pendiente", RevisionSolicitud = 9 });
        await Estado(await cliente.PostAsJsonAsync(ruta + "/condiciones", condicionEntrada with { Tipo = "Articulación", RevisionSolicitud = 10 }), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion with { Estado = "Observada", RevisionSolicitud = 10 }), HttpStatusCode.OK);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/documentos/{documento.Id}", revisionDocumento with { RevisionSolicitud = 11 }), HttpStatusCode.OK);
        await aplicacion.AsignarAccesoAsync(
            "directivo@prueba.example",
            institucion,
            RolesPersonal.Directivo);
        await iniciarSesion("directivo@prueba.example");
        await Alta<RegistrarCondicionRespuesta>(ruta + "/condiciones", condicionEntrada with { Tipo = "Articulación", RevisionSolicitud = 12 });
        comprobar((await Consultar()).RevisionSolicitud == 13 &&
            (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(ruta))!.EstadoRecepcion == "Ordinaria",
            "Tras el cierre continúan documentos/revisiones; agregar una declaración requiere permiso especial y no altera la recepción histórica.");
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(ruta + "/antecedentes"), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/documentos", documentoEntrada), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta + "/condiciones", condicionEntrada), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/documentos/{documento.Id}", revisionDocumento), HttpStatusCode.Forbidden);
        await Estado(await cliente.PutAsJsonAsync(ruta + $"/condiciones/{condicion.Id}", revisionCondicion), HttpStatusCode.Forbidden);
        await iniciarSesion("root@prueba.example");
        using var alcance = aplicacion.Services.CreateScope();
        await PruebasAntecedentesPersistencia.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
            institucion,
            solicitud,
            documento.Id,
            comprobar);
    }
}
