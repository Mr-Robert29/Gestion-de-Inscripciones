using System.Net;
using System.Net.Http.Json;
using Jardin.Configuracion.Contracts;
using Jardin.Inscripciones.Contracts;
using Jardin.Usuarios.Domain;
using Jardin.Vacantes.Contracts;
using Jardin.Vacantes.Domain.Entidades;
using Jardin.Vacantes.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jardin.Configuracion.Pruebas;

public static class PruebasListaEsperaApi
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
                throw new Exception($"HU20 esperaba {(int)esperado}; recibió {(int)respuesta.StatusCode}: {await respuesta.Content.ReadAsStringAsync()}");
            comprobar(true, $"HU20 HTTP {(int)esperado}");
        }
        async Task<T> Alta<T>(string ruta, object datos)
        {
            var respuesta = await cliente.PostAsJsonAsync(ruta, datos);
            await Estado(respuesta, HttpStatusCode.Created);
            return (await respuesta.Content.ReadFromJsonAsync<T>())!;
        }
        await iniciarSesion("root@prueba.example");
        var institucion = (await Alta<RegistrarInstitucionRespuesta>(
            "/api/v1/instituciones",
            new RegistrarInstitucionPeticion("Jardín HU20", new DatosDomicilio("Calle", "1", "Ciudad", "Provincia"), "123"))).Id;
        var rutaInstitucion = $"/api/v1/instituciones/{institucion}";
        var periodo = (await Alta<CrearPeriodoRespuesta>(rutaInstitucion + "/periodos",
            new CrearPeriodoPeticion(2027, new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1)))).Id;
        var otroPeriodo = (await Alta<CrearPeriodoRespuesta>(rutaInstitucion + "/periodos",
            new CrearPeriodoPeticion(2028, new DateOnly(2027, 11, 1), new DateOnly(2027, 12, 1)))).Id;
        var rutaPeriodo = $"/api/v1/periodos/{periodo}";
        var listaRuta = rutaInstitucion + $"/periodos/{periodo}/lista-espera";
        var vacia = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!;
        comprobar(vacia.EstadoProceso == "SinResultadosConfirmados" && vacia.PropuestaConfirmadaId is null,
            "HU20: antes de la admisión no presenta una propuesta como lista definitiva.");
        var aula = (await Alta<RegistrarAulaRespuesta>(rutaInstitucion + "/aulas", new RegistrarAulaPeticion("Aula"))).Id;
        var manana = (await Alta<RegistrarTurnoRespuesta>(rutaInstitucion + "/turnos", new RegistrarTurnoPeticion("Mañana"))).Id;
        var tarde = (await Alta<RegistrarTurnoRespuesta>(rutaInstitucion + "/turnos", new RegistrarTurnoPeticion("Tarde"))).Id;
        await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas",
            new RegistrarOfertaPeticion(aula, manana, 1, true, "Acta", "Relevamiento", [], "Carga"));
        await Alta<RegistrarOfertaRespuesta>(rutaPeriodo + "/ofertas",
            new RegistrarOfertaPeticion(aula, tarde, 0, true, "Acta", "Relevamiento", [], "Carga"));
        var reglas = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-reglas",
            new PublicarReglasPeticion(new DateOnly(2027, 6, 30), "Reglas", "Publicación"))).Id;
        var oferta = (await Alta<PublicarVersionRespuesta>(rutaPeriodo + "/versiones-oferta",
            new PublicarOfertaPeticion("Oferta", "Publicación"))).Id;
        await Estado(await cliente.PutAsJsonAsync(rutaPeriodo + "/configuracion",
            new AsignarConfiguracionPeticion(new DatosVersionesConfiguracion(reglas, oferta), 1, "Asignar")), HttpStatusCode.OK);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/abrir", new CambiarRecepcionPeticion(2, "Abrir")), HttpStatusCode.OK);
        async Task<(Guid Solicitud, Guid Aspirante)> Registrar(
            string nombre,
            string dni,
            Guid turno,
            decimal? metros,
            bool condicion)
        {
            var aspirante = (await Alta<RegistrarAspiranteRespuesta>(rutaInstitucion + "/aspirantes",
                new RegistrarAspirantePeticion(nombre, "Prueba HU20", new DateOnly(2022, 6, 30), dni))).Id;
            var solicitud = (await Alta<RegistrarSolicitudRespuesta>(rutaInstitucion + $"/periodos/{periodo}/solicitudes",
                new RegistrarSolicitudPeticion(aspirante, new DatosDomicilioSolicitud("Domicilio", "2", "Ciudad", "Provincia"), turno,
                    turno == tarde ? [manana] : []))).Id;
            if (metros is decimal valor)
                await Alta<ConsultarDistanciaRespuesta>(rutaInstitucion + $"/solicitudes/{solicitud}/distancias/manual",
                    new RegistrarDistanciaManualPeticion(valor, "Referencia manual ficticia", 1, 1, 0));
            if (condicion)
            {
                var actual = (await cliente.GetFromJsonAsync<ConsultarSolicitudRespuesta>(rutaInstitucion + $"/solicitudes/{solicitud}"))!;
                var registrada = await Alta<RegistrarCondicionRespuesta>(rutaInstitucion + $"/solicitudes/{solicitud}/condiciones",
                    new RegistrarCondicionPeticion("HijosDelPersonal", "Declarado", actual.RevisionDatos, "Registrar"));
                await Estado(await cliente.PutAsJsonAsync(rutaInstitucion + $"/solicitudes/{solicitud}/condiciones/{registrada.Id}",
                    new RevisarCondicionPeticion("Verificada", registrada.RevisionSolicitud, "Comprobado")), HttpStatusCode.OK);
            }
            return (solicitud, aspirante);
        }
        var admitido = await Registrar("Admitido", "11111111", manana, 10m, false);
        var primero = await Registrar("Primero", "22222700", manana, 20.001m, true);
        var segundo = await Registrar("Segundo", "33333500", tarde, 20.004m, true);
        await Estado(await cliente.PostAsJsonAsync(rutaPeriodo + "/cerrar-recepcion", new CambiarRecepcionPeticion(3, "Procesar")), HttpStatusCode.OK);
        var pendiente = await Registrar("Fuera de término", "44444999", manana, null, false);
        var sorteo = await Alta<DatosSorteo>(rutaPeriodo + "/sorteos", new RegistrarSorteoPeticion(347, new DateOnly(2026, 11, 2), "Acta", 5));
        Guid casoId;
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            var caso = new CasoRevision(periodo, 5, TipoCasoRevision.EmpateDistancia,
                [primero.Solicitud, segundo.Solicitud], "Caso de prueba HU20", sorteo.Id, sorteo.Version, reglas);
            caso.Resolver(Guid.NewGuid(), [primero.Solicitud, segundo.Solicitud], "Orden administrativo comprobado", 1);
            casoId = caso.Id;
            contexto.CasosRevision.Add(caso);
            await contexto.SaveChangesAsync();
        }
        var propuesta = await Alta<ConsultarPropuestaIngresosRespuesta>(rutaPeriodo + "/propuestas-ingresos", new GenerarPropuestaIngresosPeticion("Propuesta HU20"));
        comprobar((await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!.EstadoProceso == "SinResultadosConfirmados",
            "HU20: un borrador generado no publica todavía lista de espera.");
        var origenOrdenId = propuesta.Id;
        var revisado = await cliente.PatchAsJsonAsync(
            $"/api/v1/propuestas-ingresos/{propuesta.Id}",
            new ModificarPropuestaIngresosPeticion(
                propuesta.HuellaEntradas,
                "Aclaración administrativa",
                [new ModificarResultadoPropuestaPeticion(primero.Solicitud, "EsperaPropuesta", null, null, false, "Espera revisada")]));
        await Estado(revisado, HttpStatusCode.OK);
        propuesta = (await revisado.Content.ReadFromJsonAsync<ConsultarPropuestaIngresosRespuesta>())!;
        var confirmar = await cliente.PostAsJsonAsync($"/api/v1/propuestas-ingresos/{propuesta.Id}/confirmacion",
            new ConfirmarPropuestaIngresosPeticion(propuesta.HuellaEntradas));
        await Estado(confirmar, HttpStatusCode.OK);
        var ingresos = (await confirmar.Content.ReadFromJsonAsync<DatosConfirmacionIngreso[]>())!;
        comprobar(ingresos.Length == 1 && ingresos[0].AspiranteId == admitido.Aspirante,
            "HU20/HU19: el recorrido confirma un ingreso real antes de consultar la espera.");
        var lista = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!;
        comprobar(lista.TotalEnEspera == 2 && lista.TotalPendientes == 1 && lista.PropuestaConfirmadaId == propuesta.Id &&
            lista.PropuestaOrigenOrdenId == origenOrdenId,
            "HU20: separa dos aspirantes elegibles y una solicitud pendiente de autorización.");
        comprobar(lista.EnEspera.Resultados.Select(x => x.SolicitudId).SequenceEqual([primero.Solicitud, segundo.Solicitud]) &&
            lista.EnEspera.Resultados.Select(x => x.PosicionEspera).SequenceEqual([1, 2]),
            "HU20: mantiene la resolución administrativa y no recalcula el sorteo al consultar.");
        var primeroLista = lista.EnEspera.Resultados[0];
        comprobar(primeroLista.DistanciaMetrosUtilizada == 20.001m && primeroLista.DistanciaComparada == 20m &&
            primeroLista.CondicionesVerificadas.Contains("HijosDelPersonal") && primeroLista.Sorteo?.Numero == "347" &&
            primeroLista.Sorteo.ClaveCircular == 353 && primeroLista.CasosRevision.Single().PosicionDentroDelCaso == 1,
            "HU20: conserva medición, precisión de comparación, criterio verificado, sorteo y decisión manual.");
        var filtrada = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta + $"?turnoId={tarde}"))!;
        comprobar(filtrada.EnEspera.Resultados.Single().SolicitudId == segundo.Solicitud &&
            filtrada.EnEspera.Resultados.Single().PosicionEspera == 2 && filtrada.TotalEnEspera == 2,
            "HU20: el filtro de turno conserva la posición global y no crea otra lista.");
        var primeraPagina = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta + "?tamanoPagina=1"))!;
        var segundaPagina = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta + "?tamanoPagina=1&pagina=2"))!;
        comprobar(primeraPagina.EnEspera.HayMas && primeraPagina.EnEspera.Resultados.Single().SolicitudId == primero.Solicitud &&
            !segundaPagina.EnEspera.HayMas && segundaPagina.EnEspera.Resultados.Single().SolicitudId == segundo.Solicitud,
            "HU20: pagina la misma lista sin duplicar integrantes.");
        var soloPendientes = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta + "?estado=PendienteRevision"))!;
        comprobar(soloPendientes.EnEspera.TotalFiltrado == 0 && soloPendientes.Pendientes.TotalFiltrado == 1 &&
            soloPendientes.TotalEnEspera == 2, "HU20: el filtro de estado no cambia los totales ni la lista de origen.");
        var otraSala = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta + "?sala=4"))!;
        comprobar(otraSala.EnEspera.TotalFiltrado == 0 && otraSala.TotalEnEspera == 2,
            "HU20: el filtro por grupo etario no incorpora aspirantes de otra lista.");
        await Alta<ConsultarPropuestaIngresosRespuesta>(rutaPeriodo + "/propuestas-ingresos", new GenerarPropuestaIngresosPeticion("Borrador posterior"));
        comprobar((await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!.PropuestaConfirmadaId == propuesta.Id,
            "HU20: un borrador posterior no sustituye el resultado confirmado.");
        var posterior = await Registrar("Posterior", "55555123", manana, null, false);
        var conPosterior = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!;
        comprobar(conPosterior.TotalEnEspera == 2 && conPosterior.TotalPendientes == 2 &&
            conPosterior.Pendientes.Resultados.Single(x => x.SolicitudId == posterior.Solicitud).Sorteo is null &&
            !conPosterior.Pendientes.Resultados.Single(x => x.SolicitudId == posterior.Solicitud).DatosCoincidenConResultado,
            "HU20: una recepción posterior queda pendiente sin inventarle un resultado histórico del sorteo.");
        comprobar((await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(rutaInstitucion + $"/periodos/{otroPeriodo}/lista-espera"))!
            .EstadoProceso == "SinResultadosConfirmados", "HU20: no mezcla períodos de la institución.");
        await Estado(await cliente.GetAsync($"/api/v1/instituciones/{Guid.NewGuid()}/periodos/{periodo}/lista-espera"), HttpStatusCode.NotFound);
        foreach (var consulta in new[] { "sala=2", "tamanoPagina=0", "pagina=0", "estado=NoExiste", "turnoId=00000000-0000-0000-0000-000000000000" })
            await Estado(await cliente.GetAsync(listaRuta + "?" + consulta), HttpStatusCode.BadRequest);
        await aplicacion.AsignarAccesoAsync("propuestas@prueba.example", institucion, RolesPersonal.Administrativo);
        await iniciarSesion("propuestas@prueba.example");
        await Estado(await cliente.GetAsync(listaRuta), HttpStatusCode.OK);
        await iniciarSesion("propuestas-ajeno@prueba.example");
        await Estado(await cliente.GetAsync(listaRuta), HttpStatusCode.Forbidden);
        cliente.DefaultRequestHeaders.Authorization = null;
        await Estado(await cliente.GetAsync(listaRuta), HttpStatusCode.Unauthorized);
        await iniciarSesion("root@prueba.example");
        var correccion = await cliente.PostAsJsonAsync(rutaInstitucion + $"/solicitudes/{primero.Solicitud}/distancias/manual",
            new RegistrarDistanciaManualPeticion(30m, "Nueva referencia", 1, 1, 1));
        await Estado(correccion, HttpStatusCode.Created);
        var revisada = (await cliente.GetFromJsonAsync<ConsultarListaEsperaRespuesta>(listaRuta))!;
        comprobar(revisada.TotalEnEspera == 0 && revisada.TotalPendientes == 4 && revisada.RequiereRevision &&
            revisada.Pendientes.Resultados.Single(x => x.SolicitudId == primero.Solicitud).DistanciaMetrosUtilizada == 20.001m,
            "HU20: una corrección posterior exige revisión y conserva el dato utilizado en admisión.");
        using (var alcance = aplicacion.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoVacantes>();
            comprobar(await contexto.IngresosConfirmados.CountAsync(x => x.PeriodoId == periodo) == 1 &&
                await contexto.PropuestasIngresos.CountAsync(x => x.PeriodoId == periodo) == 3 &&
                await contexto.EvaluacionesPrioridad.CountAsync(x => x.PeriodoId == periodo) == 0,
                "HU20: consultar y filtrar no confirma, no guarda nuevas propuestas ni genera evaluaciones.");
        }
    }
}
