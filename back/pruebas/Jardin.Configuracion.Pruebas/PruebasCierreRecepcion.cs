using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;

namespace Jardin.Configuracion.Pruebas;

// Reutiliza el escenario de HU-07 para comprobar el cierre con sus solicitudes y versiones reales.
public sealed record EscenarioCierreRecepcion(
    Guid InstitucionId,
    Guid PeriodoId,
    Guid SolicitudOrdinariaId,
    Guid SolicitudFueraId,
    Guid VersionReglasOriginalId,
    Guid VersionReglasActualId,
    Guid VersionOfertaId,
    RegistrarSolicitudPeticion Entrada);

public static class PruebasCierreRecepcion
{
    public static async Task EjecutarAsync(
        HttpClient cliente,
        Func<string, Task> iniciarSesion,
        EscenarioCierreRecepcion escenario,
        Action<bool, string> comprobar)
    {
        async Task Estado(HttpResponseMessage respuesta, HttpStatusCode esperado)
        {
            if (respuesta.StatusCode != esperado)
                throw new Exception($"Cierre HU-13: esperado {(int)esperado}, recibido {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"Cierre HU-13 HTTP {(int)esperado}");
        }
        var raiz = $"/api/v1/instituciones/{escenario.InstitucionId}";
        var rutaPeriodo = $"/api/v1/periodos/{escenario.PeriodoId}";
        var rutaSolicitud = raiz + $"/solicitudes/{escenario.SolicitudOrdinariaId}";
        var periodo = (await cliente.GetFromJsonAsync<ConsultarPeriodoRespuesta>(rutaPeriodo))!;
        comprobar(periodo.Estado == "RecepcionCerrada" && periodo.VersionReglasId == escenario.VersionReglasActualId &&
            periodo.VersionOfertaId == escenario.VersionOfertaId,
            "El período cerrado conserva las versiones explícitamente asociadas para continuar el procesamiento.");
        var ordinaria = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!;
        var fuera = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(raiz + $"/solicitudes/{escenario.SolicitudFueraId}"))!;
        comprobar(ordinaria.EstadoRecepcion == "Ordinaria" && fuera.EstadoRecepcion == "FueraDeTerminoPendienteAutorizacion" &&
            ordinaria.VersionReglasId == escenario.VersionReglasOriginalId && fuera.VersionReglasId == escenario.VersionReglasOriginalId,
            "Cerrar o reasignar versiones no reclasifica ni recalcula las solicitudes ya recibidas.");
        var cambiosAntes = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>(raiz + "/historial"))!;
        bool EsCierreDelPeriodo(DatosCambioInstitucion cambio)
        {
            if (cambio.Accion != "Periodo.CierreRecepcion") return false;
            using var datos = JsonDocument.Parse(cambio.DatosNuevos);
            return datos.RootElement.GetProperty("Id").GetGuid() == escenario.PeriodoId;
        }
        var cierres = cambiosAntes.Where(EsCierreDelPeriodo).ToArray();
        comprobar(cierres.Length == 1 && cierres[0].UsuarioId != Guid.Empty && cierres[0].Fecha != default &&
            cierres[0].Motivo == "Cerrar" && cierres[0].DatosAnteriores is not null,
            "El cierre conserva autor, fecha, motivo y estado anterior/nuevo, sin perder trazabilidad.");
        var cierre = new CambiarRecepcionPeticion(periodo.Revision, "Intento de segundo cierre");
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", cierre), HttpStatusCode.Conflict);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/abrir", cierre), HttpStatusCode.Conflict);
        var cambiosDespues = (await cliente.GetFromJsonAsync<DatosCambioInstitucion[]>(raiz + "/historial"))!;
        comprobar(cambiosDespues.Length == cambiosAntes.Length &&
            (await cliente.GetFromJsonAsync<ConsultarPeriodoRespuesta>(rutaPeriodo))!.Revision == periodo.Revision,
            "Repetir el cierre o intentar reabrir no altera el período ni agrega historial ficticio.");
        var respuesta = await cliente.PostAsJsonAsync(raiz + $"/periodos/{escenario.PeriodoId}/solicitudes", escenario.Entrada);
        await Estado(respuesta, HttpStatusCode.Created);
        var nueva = (await respuesta.Content.ReadFromJsonAsync<RegistrarSolicitudRespuesta>())!;
        var recibida = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(raiz + $"/solicitudes/{nueva.Id}"))!;
        comprobar(recibida.FueraDeTermino && recibida.EstadoRecepcion == "FueraDeTerminoPendienteAutorizacion" &&
            recibida.VersionReglasId == escenario.VersionReglasActualId && recibida.VersionOfertaId == escenario.VersionOfertaId,
            "Después del cierre se conserva la recepción pendiente con las versiones actuales, sin autorizarla automáticamente.");
        var documento = await cliente.PostAsJsonAsync(rutaSolicitud + "/documentos", new RegistrarDocumentoPeticion(
            "Constancia ficticia HU13",
            "Pendiente",
            ordinaria.RevisionDatos,
            "Continuar revisión tras cierre"));
        await Estado(documento, HttpStatusCode.Created);
        var documentoId = (await documento.Content.ReadFromJsonAsync<RegistrarDocumentoRespuesta>())!.Id;
        await Estado(await cliente.PutAsJsonAsync(rutaSolicitud + $"/documentos/{documentoId}", new RevisarDocumentoPeticion(
            "Presentada",
            ordinaria.RevisionDatos + 1,
            "Constancia recibida después del cierre")), HttpStatusCode.OK);
        comprobar((await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaSolicitud))!.EstadoRecepcion == "Ordinaria" &&
            (await cliente.GetFromJsonAsync<ConsultarPeriodoRespuesta>(rutaPeriodo))!.Estado == "RecepcionCerrada",
            "La revisión documental sigue disponible sin reabrir recepción ni cambiar el origen de la solicitud.");
        await iniciarSesion("solicitudes@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", cierre), HttpStatusCode.Forbidden);
        await iniciarSesion("ajeno@prueba.example");
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", cierre), HttpStatusCode.Forbidden);
        var autorizacion = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", cierre), HttpStatusCode.Unauthorized);
        cliente.DefaultRequestHeaders.Authorization = autorizacion;
        await iniciarSesion("root@prueba.example");
    }
}
