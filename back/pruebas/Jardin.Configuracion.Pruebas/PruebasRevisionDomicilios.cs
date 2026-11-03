using System.Net;
using System.Net.Http.Json;
using Jardin.Inscripciones.Contracts;
using Jardin.Inscripciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasRevisionDomicilios
{
    // Continúa sobre la solicitud cerrada preparada por HU-11, con domicilio y distancia reales de esa prueba.
    public static async Task EjecutarAsync(
        HttpClient cliente,
        ApiPruebas aplicacion,
        Func<string, Task> iniciarSesion,
        Guid institucion,
        Guid solicitud,
        Action<bool, string> comprobar)
    {
        async Task Estado(HttpResponseMessage respuesta, HttpStatusCode esperado)
        {
            if (respuesta.StatusCode != esperado)
                throw new Exception($"Revisión domicilio: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Revisión domicilio HTTP {(int)esperado}");
        }
        var raiz = $"/api/v1/instituciones/{institucion}";
        var rutaSolicitud = raiz + $"/solicitudes/{solicitud}";
        var ruta = rutaSolicitud + "/revision-domicilio";
        async Task<ConsultarRevisionDomicilioRespuesta> Consultar() => (await cliente.GetFromJsonAsync<ConsultarRevisionDomicilioRespuesta>(ruta))!;
        async Task<ConsultarRevisionDomicilioRespuesta> Registrar(RegistrarRevisionDomicilioPeticion datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<ConsultarRevisionDomicilioRespuesta>())!;
        }
        var inicial = await Consultar();
        comprobar(inicial.Estado == "PendienteRevision" && inicial.RevisionActual is null && inicial.Historial.Count == 0 && inicial.UltimoNumeroRegistro == 0,
            "Una dirección sin revisión queda pendiente, sin inventar un revisor ni un historial.");
        var peticion = new RegistrarRevisionDomicilioPeticion(
            "Observado",
            inicial.RevisionSolicitud,
            inicial.RevisionDomicilio,
            0,
            "Falta aclarar la numeración declarada");
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Estado = "1" }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Estado = "ValidadoPorGeoapify" }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Motivo = " " }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { RevisionSolicitud = 0 }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { UltimoNumeroRegistro = -1 }), HttpStatusCode.BadRequest);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { RevisionDomicilio = inicial.RevisionDomicilio + 1 }), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion with { Estado = "RevisadoManualmente" }), HttpStatusCode.BadRequest);
        await iniciarSesion("distancias@prueba.example");
        var observada = await Registrar(peticion);
        comprobar(observada.Estado == "Observado" && observada.RevisionActual!.UsuarioId != Guid.Empty && observada.RevisionActual.Fecha != default &&
            observada.RevisionActual.Motivo == peticion.Motivo && observada.RevisionSolicitud == inicial.RevisionSolicitud + 1 &&
            observada.RevisionDomicilio == inicial.RevisionDomicilio,
            "El Administrativo puede observar después del cierre con motivo, usuario y fecha, sin modificar la dirección.");
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion), HttpStatusCode.Conflict);
        var validacion = peticion with
        {
            Estado = "RevisadoManualmente",
            RevisionSolicitud = observada.RevisionSolicitud,
            UltimoNumeroRegistro = 1,
            Motivo = "Se comprobó la dirección declarada",
            Referencia = "Entrevista presencial ficticia con el responsable"
        };
        var revisada = await Registrar(validacion);
        comprobar(revisada.Estado == "RevisadoManualmente" && revisada.Historial.Count == 2 && revisada.Historial[0].EsActual &&
            !revisada.Historial[1].EsActual && revisada.RevisionActual!.Referencia == validacion.Referencia,
            "La revisión manual conserva la observación anterior y exige una referencia de comprobación.");
        var medidas = (await cliente.GetFromJsonAsync<ConsultarDistanciaRespuesta[]>(rutaSolicitud + "/distancias"))!;
        comprobar(medidas.Length == 5 && medidas[0].Origen == "ManualProvisional",
            "Revisar el domicilio no genera otra distancia ni transforma la carga manual en un cálculo automático.");
        await iniciarSesion("root@prueba.example");
        var datos = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!;
        var calleAnterior = datos.Domicilio.Calle;
        await Estado(await cliente.PutAsJsonAsync(rutaSolicitud, new CorregirSolicitudPeticion(
            datos.Domicilio with { Calle = "Dirección nueva HU12" },
            datos.TurnoPreferidoId,
            datos.TurnosAlternativosAceptados,
            datos.RevisionDatos,
            "Corregir el domicilio revisado",
            datos.ReferenciaConsentimiento)), HttpStatusCode.OK);
        var modificada = await Consultar();
        comprobar(modificada.Estado == "PendienteRevision" && modificada.RevisionActual is null && modificada.Historial.All(r => !r.EsActual) &&
            modificada.Historial.All(r => r.Domicilio.Calle == calleAnterior) && modificada.RevisionDomicilio == inicial.RevisionDomicilio + 1,
            "Corregir la dirección reinicia la revisión actual y conserva intactas las decisiones de la dirección anterior.");
        comprobar((await cliente.GetFromJsonAsync<ConsultarDistanciaRespuesta[]>(rutaSolicitud + "/distancias"))!.All(m => !m.EsVigente),
            "La corrección reutiliza HU-09 y deja las distancias anteriores desactualizadas.");
        await Estado(await cliente.PostAsJsonAsync(ruta, validacion), HttpStatusCode.Conflict);
        var pendiente = await Registrar(peticion with
        {
            Estado = "PendienteRevision",
            RevisionSolicitud = modificada.RevisionSolicitud,
            RevisionDomicilio = modificada.RevisionDomicilio,
            UltimoNumeroRegistro = 2,
            Motivo = "Pendiente de comprobación presencial"
        });
        var nueva = await Registrar(validacion with
        {
            RevisionSolicitud = pendiente.RevisionSolicitud,
            RevisionDomicilio = pendiente.RevisionDomicilio,
            UltimoNumeroRegistro = 3,
            Referencia = "Nueva comprobación presencial ficticia"
        });
        datos = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!;
        await Estado(await cliente.PutAsJsonAsync(rutaSolicitud, new CorregirSolicitudPeticion(
            datos.Domicilio,
            datos.TurnoPreferidoId,
            datos.TurnosAlternativosAceptados,
            datos.RevisionDatos,
            "Actualizar referencia de consentimiento",
            "Referencia cambiada sin modificar la dirección")), HttpStatusCode.OK);
        comprobar((await Consultar()).RevisionActual!.Id == nueva.RevisionActual!.Id &&
            (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!.EstadoRecepcion == "Ordinaria",
            "Un cambio ajeno al domicilio conserva su revisión y la solicitud mantiene su recepción original.");
        var cambios = (await cliente.GetFromJsonAsync<DatosCambioInscripciones[]>(raiz + "/personas/historial"))!;
        comprobar(cambios.Count(c => c.Accion == "Domicilio.RevisionManual") == 4 &&
            cambios.Any(c => c.Datos.Contains("Observado") && c.Datos.Contains("RevisadoManualmente")),
            "Cada decisión manual conserva auditoría y referencia a la decisión que reemplaza.");
        await Estado(await cliente.GetAsync($"/api/v1/instituciones/{Guid.NewGuid()}/solicitudes/{solicitud}/revision-domicilio"), HttpStatusCode.NotFound);
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.GetAsync(ruta), HttpStatusCode.Forbidden);
        await Estado(await cliente.PostAsJsonAsync(ruta, peticion), HttpStatusCode.Forbidden);
        var autorizacion = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(ruta), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = autorizacion;
        await iniciarSesion("root@prueba.example");
        using var alcance = aplicacion.Services.CreateScope();
        await PruebasRevisionDomiciliosPersistencia.EjecutarAsync(
            alcance.ServiceProvider.GetRequiredService<DbContextOptions<ContextoInscripciones>>(),
            institucion,
            solicitud,
            comprobar);
    }
}
