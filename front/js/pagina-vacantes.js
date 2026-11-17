import { escapar as e, etiqueta, fecha, seleccion } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import { candidatosDeEtapa, cohortePrioridad, cumpleCriterio } from "./prioridades-etapas.js";
import {
  rutaPeriodo,
  rutaInstitucional,
  nombreTurno,
  nombreAula,
  nombreSolicitud,
  enlaceSolicitud,
  boton,
  tabla,
  pestanas,
  resumen,
  cargarNombres,
  vistaPreviaOrganizacion,
  historial,
} from "./organizacion-comun.js";

const etapas = [
  ["vacantes", "Propuestas"],
  ["vacantes/prioridades", "Prioridades"],
  ["vacantes/espera", "Lista de espera"],
  ["vacantes/ofrecimientos", "Ofrecimientos"],
  ["vacantes/ingresos", "Ingresos y bajas"],
];
const institucionDeclarada = (condicion) => {
  const prefijo = "Institución de procedencia informada por la familia: ";
  const referencia = condicion.evidenciaReferencia ?? "";
  return referencia.startsWith(prefijo)
    ? referencia.slice(prefijo.length)
    : "No informada";
};
const criterios = [
  ["ArticulacionPedagogica", "1. Articulación pedagógica"],
  ["HermanosYGruposConvivientes", "2. Hermanos o grupos convivientes"],
  ["Cercania", "3. Cercanía al jardín"],
  ["HijosDelPersonal", "4. Hijos del personal"],
];
const etapaDe = (resultado) => resultado.explicacion?.includes("Solicitud fuera de término.")
  ? "FueraDeTermino"
  : cohortePrioridad(resultado);
const nombreEtapa = (prioridad) => {
  if (!prioridad) return "Sin evaluación vinculada";
  if (prioridad.explicacion?.includes("Solicitud fuera de término.")) return "Fuera de término";
  if (cumpleCriterio(prioridad, "ArticulacionPedagogica")) return "Articulación pedagógica";
  if (cumpleCriterio(prioridad, "HermanosYGruposConvivientes")) return "Hermanos o grupo conviviente";
  const cercania = prioridad.explicacion?.find((texto) => texto.startsWith("Cercanía:"));
  if (cercania) return cercania.replace("Cercanía:", "Cercanía ·").replace(" metros.", " m");
  if (cumpleCriterio(prioridad, "HijosDelPersonal")) return "Hijos del personal";
  if (prioridad.requiereRevision) return "Requiere revisión";
  return "Sin prioridad acreditada";
};
const estadoPropuestaLegible = (estado) => ({
  IngresoPropuesto: "Ingreso propuesto",
  EsperaPropuesta: "En lista de espera",
  PendienteRevision: "Requiere revisión",
  YaOcupaPlaza: "Ya tiene un ingreso",
})[estado] ?? estado;
export async function vacantes(ruta, ctx) {
  const paso = ruta.split("/")[1] ?? "";
  const activa = ["propuesta"].includes(paso)
    ? "vacantes"
    : `vacantes${paso ? `/${paso}` : ""}`;
  const cabecera =
    encabezado({
      titulo: "Vacantes y admisión",
      descripcion: "Revisá cada etapa antes de confirmar las decisiones.",
    }) + pestanas("admisión", activa, etapas);
  if (ctx.estado.previa) return cabecera + vistaPreviaOrganizacion();
  await cargarNombres(ctx);
  const p = rutaPeriodo(ctx);
  if (paso === "prioridades") {
    const [estadoProcesamiento, sorteos, casos, articulaciones, propuestas] = await Promise.all([
      ctx.api(`${p}/prioridades/estado`),
      ctx.api(`${p}/sorteos`),
      ctx.api(`${p}/casos-revision`).catch((error) => {
        if (error.estado !== 404) throw error;
        return { aviso: error.message };
      }),
      ctx.api(`${rutaInstitucional(ctx)}/articulaciones`),
      ctx.api(`${p}/propuestas-ingresos`).catch(() => []),
    ]);
    const propuestaVigente = propuestas
      .filter((x) => x.entradasVigentes && x.estado !== "Desactualizada")
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))[0] ?? null;
    const prioridades = estadoProcesamiento.vigente ? estadoProcesamiento.resultados : [];
    const pendientesRevision = estadoProcesamiento.pendientesRevision ?? articulaciones.filter((x) => ["PendienteRevision", "Observada"].includes(x.condicion.estadoVerificacion)).length;
    const puedeProcesar = ctx.puedeResolver() && estadoProcesamiento.recepcionCerrada && pendientesRevision === 0 && !estadoProcesamiento.vigente;
    ctx.organizacion = {
      prioridades,
      estadoProcesamiento,
      sorteos,
      casos: Array.isArray(casos) ? casos : [],
      articulaciones,
      propuestaVigente,
    };
    return (
      cabecera +
      resumen(
        "Prioridades por revisar",
        `<p>Las respuestas afirmativas requieren verificación institucional antes de aplicar una prioridad. Las decisiones quedan registradas en el historial de la solicitud.</p>` +
          [
            ["articulacion", "Articulaciones por revisar", ["ArticulacionPedagogica", "ConsultaArticulacionPedagogica"], "Institución declarada"],
            ["hermanos", "Hermanos o grupos convivientes por verificar", ["HermanosYGruposConvivientes"], "Dato declarado"],
            ["personal", "Hijos del personal por verificar", ["HijosDelPersonal"], "Dato declarado"],
          ].map(([grupo, titulo, tipos, columna]) => {
            const pendientes = articulaciones.filter((x) => tipos.includes(x.condicion.tipo) && ["PendienteRevision", "Observada"].includes(x.condicion.estadoVerificacion));
            const paraConfirmar = pendientes.filter((x) => x.condicion.estadoVerificacion === "PendienteRevision");
            const botonConfirmar = ctx.puedeResolver()
              ? `<button class="boton boton--secundario boton--pequeno" data-accion="org-confirmar-prioridades-lote" data-id="${e(grupo)}" ${paraConfirmar.length ? "" : "disabled"}>Confirmar todos (${paraConfirmar.length})</button>`
              : "";
            return `<section class="prioridades__grupo"><div class="prioridades__grupo-cabecera"><h3>${e(titulo)}</h3>${botonConfirmar}</div>${tabla(
              ["Aspirante", columna, "Respuesta", "Estado", "Acción"],
              pendientes.map((x) => [
                enlaceSolicitud(ctx, x.solicitudId),
                e(x.condicion.tipo === "ArticulacionPedagogica" || x.condicion.tipo === "ConsultaArticulacionPedagogica" ? institucionDeclarada(x.condicion) : x.condicion.evidenciaReferencia ?? "Sin detalle"),
                e(x.condicion.valorDeclarado),
                etiqueta(x.condicion.estadoVerificacion),
                ctx.puedeResolver() ? boton("org-revisar-articulacion", "Revisar", x.condicion.id) : "Requiere Directivo",
              ]),
            )}${pendientes.some((x) => x.condicion.estadoVerificacion === "Observada") ? '<p class="formulario__nota">Los casos observados requieren revisión individual y no se incluyen en la confirmación rápida.</p>' : ""}</section>`;
          }).join("") +
          (ctx.estado.periodo.estado !== "RecepcionCerrada"
            ? '<p class="formulario__nota">Podés confirmar las declaraciones durante la recepción. La decisión “No acreditada al cierre” solo estará disponible después de cerrar el período.</p>'
            : ""),
      ) +
      resumen(
        "Etapas de prioridad",
        `<p>Primero se procesa la lista con los criterios en orden. Después, en <strong>Propuestas</strong>, se genera una propuesta de ingreso para revisar. El procesamiento ordena; no confirma vacantes.</p>` +
          (!estadoProcesamiento.recepcionCerrada
            ? '<div class="aviso aviso--aviso"><p>Cerrá la recepción para preparar y procesar la lista de aspirantes.</p></div>'
            : pendientesRevision > 0
              ? `<div class="aviso aviso--aviso"><p>La lista todavía no está lista: quedan <strong>${pendientesRevision}</strong> prioridades por revisar. Resolvé esos casos antes de procesarla.</p></div>`
              : estadoProcesamiento.vigente
                ? `<div class="aviso"><p>Lista procesada: ${estadoProcesamiento.cantidadSolicitudes} solicitudes. Si se modifica una prioridad, un domicilio o una regla, vas a tener que procesarla nuevamente.</p></div>`
                : estadoProcesamiento.procesado
                  ? `<div class="aviso aviso--aviso"><p>El último procesamiento quedó desactualizado. Volvé a procesar la lista para generar una propuesta con los datos actuales.</p></div>`
                  : '<div class="aviso"><p>La lista todavía no fue procesada. Las etapas aparecerán cuando el Directivo inicie el procesamiento.</p></div>') +
          (puedeProcesar
            ? boton("org-procesar-prioridades", estadoProcesamiento.procesado ? "Procesar lista nuevamente" : "Procesar lista de aspirantes")
            : "") +
          `<div class="prioridades__etapas">${criterios.map(([clave, titulo], indice) => {
            const candidatos = candidatosDeEtapa(prioridades, clave);
            const conPrioridad = clave === "Cercania"
              ? candidatos
              : candidatos.filter((x) => cumpleCriterio(x, clave));
            const siguen = clave === "ArticulacionPedagogica" || clave === "HermanosYGruposConvivientes"
              ? candidatos.length - conPrioridad.length
              : null;
            const propuestasEtapa = propuestaVigente?.resultados.filter((x) => {
              if (x.estado !== "IngresoPropuesto") return false;
              const prioridad = prioridades.find((r) => r.solicitudId === x.solicitudId);
              if (!prioridad) return false;
              return clave === "HijosDelPersonal"
                ? etapaDe(prioridad) === "Cercania" && cumpleCriterio(prioridad, clave)
                : etapaDe(prioridad) === clave;
            }).length ?? 0;
            const detalle = estadoProcesamiento.vigente
              ? clave === "Cercania"
                ? `${candidatos.length} en orden por cercanía · ${propuestaVigente ? `${propuestasEtapa} propuestas` : "propuesta pendiente"}`
                : `${conPrioridad.length} con prioridad verificada${siguen !== null ? ` · ${siguen} continúan` : " · criterio de desempate dentro del grupo restante"} · ${propuestaVigente ? `${propuestasEtapa} propuestas` : "propuesta pendiente"}`
              : "Pendiente de procesamiento";
            return `<button class="prioridades__etapa" data-accion="org-ver-etapa" data-id="${e(clave)}" ${estadoProcesamiento.vigente ? "" : "disabled"}><span class="prioridades__numero">${indice + 1}</span><span class="prioridades__texto"><strong>${e(titulo)}</strong><small>${detalle}</small></span><span class="prioridades__flecha" aria-hidden="true">›</span></button>`;
          }).join("")}</div>` +
          `<button class="prioridades__etapa" data-accion="org-ver-etapa" data-id="SorteoUltimaInstancia" ${estadoProcesamiento.vigente ? "" : "disabled"}><span class="prioridades__numero">5</span><span class="prioridades__texto"><strong>Sorteo, última instancia</strong><small>${estadoProcesamiento.vigente ? `${sorteos.length} sorteos registrados` : "Pendiente de procesamiento"}</small></span><span class="prioridades__flecha" aria-hidden="true">›</span></button>` +
          `<button class="prioridades__etapa" data-accion="org-ver-etapa" data-id="FueraDeTermino" ${estadoProcesamiento.vigente ? "" : "disabled"}><span class="prioridades__numero">6</span><span class="prioridades__texto"><strong>Inscripciones fuera de término</strong><small>${estadoProcesamiento.vigente ? `${prioridades.filter((x) => x.explicacion?.includes("Solicitud fuera de término.")).length} solicitudes` : "Pendiente de procesamiento"}</small></span><span class="prioridades__flecha" aria-hidden="true">›</span></button>`,
      ) +
      resumen(
        "Sorteos registrados",
        tabla(
          ["Sala", "Número inicial", "Fecha", "Referencia", "Recorrido"],
          sorteos.map((x) => [
            e(x.sala),
            e(String(x.numero).padStart(3, "0")),
            e(fecha(x.fecha)),
            e(x.referencia),
            boton("org-orden", "Ver orden", x.sala),
          ]),
        ),
        boton("org-sorteo", "Registrar sorteo"),
      ) +
      resumen(
        "Casos para revisión manual",
        (casos.aviso
          ? `<p>${e(casos.aviso)} Registrá el sorteo correspondiente y volvé a consultar.</p>`
          : "") +
          tabla(
            ["Tipo / sala", "Estado", "Participantes", "Acciones"],
            (Array.isArray(casos) ? casos : []).map((x) => [
              e(`${x.tipo} · ${x.sala} años`),
              etiqueta(x.estado),
              x.solicitudesAfectadas
                .map((id) => enlaceSolicitud(ctx, id))
                .join("<br>"),
              boton("org-caso-historial", "Historial", x.id) +
                (ctx.puedeResolver()
                  ? boton(
                      x.estado === "Resuelto" ? "org-reabrir" : "org-resolver",
                      x.estado === "Resuelto" ? "Reabrir" : "Resolver",
                      x.id,
                    )
                  : ""),
            ]),
          ),
      ) +
      '<p class="formulario__nota">El sorteo se aplica en su instancia y grupo correspondientes. Los casos empatados requieren una decisión justificada.</p>'
    );
  }
  if (paso === "candidatos") {
    const ofertaId = ruta.split("/")[2];
    const lista = await ctx.api(
      `${p}/ofertas/${encodeURIComponent(ofertaId)}/candidatos`,
    );
    ctx.candidatosOferta = { ...lista, operacionId: crypto.randomUUID() };
    return (
      cabecera +
      resumen(
        "Candidatos compatibles",
        tabla(
          ["Aspirante", "Sala", "Posición en espera", "Acción"],
          lista.candidatos.map((x) => [
            enlaceSolicitud(ctx, x.solicitudId),
            e(x.sala),
            e(x.posicionEspera),
            boton("org-ofrecer", "Ofrecer plaza", x.solicitudId),
          ]),
        ),
      ) +
      '<p class="formulario__nota">Se conserva el orden general. Elegir un candidato requiere motivo y el servidor vuelve a comprobar la disponibilidad.</p>'
    );
  }
  if (paso === "propuesta") {
    const propuesta = await ctx.api(
      `/propuestas-ingresos/${encodeURIComponent(ruta.split("/")[2])}`,
    );
    if (propuesta.periodoId !== ctx.estado.periodo.id)
      throw new Error(
        "La propuesta pertenece a otro período. Seleccioná su contexto.",
      );
    const evaluaciones = await ctx.api(`${p}/prioridades/historial`).catch(() => []);
    const evaluacionPropuesta = evaluaciones
      .filter((x) => x.versionReglasId === propuesta.versionReglasId && new Date(x.fecha) <= new Date(propuesta.fecha))
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))[0] ?? null;
    const aceptacionesTurno = await ctx.api(`/propuestas-ingresos/${encodeURIComponent(propuesta.id)}/aceptaciones-turno`);
    const solicitudesConAceptacion = new Set(aceptacionesTurno.map((x) => x.solicitudId));
    const prioridades = evaluacionPropuesta?.resultados ?? [];
    const prioridadPorSolicitud = new Map(prioridades.map((x) => [x.solicitudId, x]));
    const ofertas = ctx.estado.ofertasPublicadas ?? [];
    const ofertaPorId = new Map(ofertas.map((x) => [x.id, x]));
    const propuestos = propuesta.resultados.filter((x) => x.estado === "IngresoPropuesto");
    const enEspera = propuesta.resultados.filter((x) => x.estado === "EsperaPropuesta");
    const pendientes = propuesta.resultados.filter((x) => x.estado === "PendienteRevision");
    const capacidadTotal = ofertas.reduce((total, x) => total + Number(x.capacidadNetaInicial ?? x.capacidad ?? 0), 0);
    const cuposRestantes = Math.max(0, capacidadTotal - propuestos.length);
    const metricas = `<div class="propuesta__metricas"><article><span>Solicitudes evaluadas</span><strong>${propuesta.resultados.length}</strong></article><article><span>Ingresos propuestos</span><strong>${propuestos.length}</strong></article><article><span>En lista de espera</span><strong>${enEspera.length}</strong></article><article><span>Requieren revisión</span><strong>${pendientes.length}</strong></article><article><span>Cupos restantes</span><strong>${cuposRestantes}</strong></article></div>`;
    const etapasResumen = [
      ["ArticulacionPedagogica", "Articulación pedagógica", (p) => cumpleCriterio(p, "ArticulacionPedagogica")],
      ["HermanosYGruposConvivientes", "Hermanos o grupo conviviente", (p) => etapaDe(p) === "HermanosYGruposConvivientes"],
      ["Cercania", "Cercanía", (p) => etapaDe(p) === "Cercania"],
      ["HijosDelPersonal", "Hijos del personal · desempate", (p) => etapaDe(p) === "Cercania" && cumpleCriterio(p, "HijosDelPersonal")],
      ["FueraDeTermino", "Fuera de término", (p) => etapaDe(p) === "FueraDeTermino"],
    ];
    const resumenEtapas = tabla(
      ["Prioridad considerada", "Ingresos propuestos", "En espera", "Requieren revisión"],
      etapasResumen.map(([clave, titulo, coincide]) => {
        const ids = new Set(prioridades.filter(coincide).map((x) => x.solicitudId));
        return [
          e(titulo),
          propuestos.filter((x) => ids.has(x.solicitudId)).length,
          enEspera.filter((x) => ids.has(x.solicitudId)).length,
          pendientes.filter((x) => ids.has(x.solicitudId)).length,
        ];
      }),
    ) + `<p class="formulario__nota">Hijos del personal se muestra como desempate dentro del grupo de cercanía; sus cantidades pueden estar incluidas también en esa etapa.${evaluacionPropuesta ? "" : " No se encontró una evaluación de prioridades vinculada a esta propuesta, por eso no se puede reconstruir esa distribución."}</p>`;
    const cuposPorOferta = tabla(
      ["Oferta propuesta", "Cupos publicados", "Propuestas", "Restantes"],
      ofertas.map((oferta) => {
        const asignados = propuestos.filter((x) => x.ofertaId === oferta.id).length;
        return [
          e(`${nombreAula(ctx, oferta.aulaId)} · ${nombreTurno(ctx, oferta.turnoId)}`),
          e(Number(oferta.capacidadNetaInicial ?? oferta.capacidad ?? 0)),
          e(asignados),
          e(Math.max(0, Number(oferta.capacidadNetaInicial ?? oferta.capacidad ?? 0) - asignados)),
        ];
      }),
    );
    ctx.organizacion = { propuesta, prioridades };
    return (
      cabecera +
      resumen(
        `Propuesta de ingresos · Ciclo ${e(ctx.estado.periodo.ciclo)}`,
        `<div class="propuesta__cabecera"><div><p>${etiqueta(propuesta.estado)} · ${propuesta.entradasVigentes ? "Datos vigentes" : "Requiere revisión"} · Generada el ${e(fecha(propuesta.fecha))}</p><p>${e(propuesta.motivo)}</p></div></div><div class="aviso aviso--aviso"><p>Esta es una propuesta preliminar. Revisá los resultados antes de confirmar; los ingresos se registran definitivamente al confirmar.</p></div>${metricas}`,
        boton("org-confirmar-propuesta", "Confirmar ingresos", propuesta.id, !ctx.puedeResolver() || propuesta.estado === "Confirmada" || !propuesta.entradasVigentes || propuestos.length === 0),
      ) +
      resumen(
        "Distribución de la propuesta por prioridad",
        resumenEtapas,
      ) +
      resumen(
        "Cupos por oferta de aula y turno",
        cuposPorOferta + '<p class="formulario__nota">La columna “restantes” descuenta los ingresos propuestos de la capacidad neta publicada. Una propuesta todavía no ocupa definitivamente esos cupos.</p>',
      ) +
      resumen(
        "Resultado por aspirante",
        `<div class="propuesta__filtro"><label for="filtro-aceptacion-turno">Filtrar aceptación familiar</label><select id="filtro-aceptacion-turno" class="campo__control" data-filtro-aceptacion-turno><option value="todas">Todos los aspirantes</option><option value="pendiente">Requieren aceptación (${propuesta.resultados.filter((x) => x.estado === "IngresoPropuesto" && x.esTurnoAlternativo && !solicitudesConAceptacion.has(x.solicitudId)).length})</option><option value="registrada">Aceptación registrada</option></select><span data-resumen-aceptacion-turno aria-live="polite">${propuesta.resultados.filter((x) => x.estado === "IngresoPropuesto" && x.esTurnoAlternativo && !solicitudesConAceptacion.has(x.solicitudId)).length} pendientes de registrar</span></div><div class="propuesta__tabla">${tabla(
          [
            "Aspirante",
            "Grupo etario",
            "Oferta propuesta",
            "Prioridad considerada",
            "Resultado",
            "Acciones",
          ],
          propuesta.resultados.map((x) => {
            const estadoAceptacion = x.estado !== "IngresoPropuesto" || !x.esTurnoAlternativo
              ? "no-aplica"
              : solicitudesConAceptacion.has(x.solicitudId)
                ? "registrada"
                : "pendiente";
            const prioridad = prioridadPorSolicitud.get(x.solicitudId);
            const oferta = ofertaPorId.get(x.ofertaId);
            const etapa = nombreEtapa(prioridad);
            const personalComoDesempate = prioridad && etapa.startsWith("Cercanía") && cumpleCriterio(prioridad, "HijosDelPersonal");
            const ofertaTexto = oferta
              ? `${nombreAula(ctx, oferta.aulaId)} · ${nombreTurno(ctx, oferta.turnoId)}${x.esTurnoAlternativo ? " · turno alternativo" : ""}`
              : "Sin oferta asignada";
            const accionesFila = boton("org-ver-resultado-propuesta", "Ver detalle", x.solicitudId) +
              (ctx.puedeResolver() && propuesta.estado !== "Confirmada"
                ? boton("org-modificar-propuesta", "Revisar", x.solicitudId)
                : "") +
              (x.esTurnoAlternativo && x.estado === "IngresoPropuesto" && propuesta.estado !== "Confirmada"
                ? estadoAceptacion === "registrada"
                  ? '<span class="propuesta__aceptacion propuesta__aceptacion--registrada">Aceptación registrada</span>'
                  : boton("org-aceptacion", "Registrar aceptación", x.solicitudId)
                : "");
            const fila = [
              enlaceSolicitud(ctx, x.solicitudId),
              e(x.sala ? `Sala de ${x.sala} años` : "Pendiente"),
              e(ofertaTexto),
              `<span>${e(etapa)}${personalComoDesempate ? '<small class="propuesta__nota-etapa">Hijo/a del personal verificado · criterio de desempate</small>' : ""}</span>`,
              `<span class="propuesta__estado propuesta__estado--${e(x.estado.toLowerCase())}">${e(estadoPropuestaLegible(x.estado))}</span>`,
              accionesFila,
            ];
            fila.aceptacion = estadoAceptacion;
            return fila;
          }),
          { estadoFila: (fila) => fila.aceptacion },
        )}</div>`,
      ) +
      `<p class="formulario__nota">Los turnos alternativos requieren aceptación familiar. La propuesta no confirma ingresos hasta que el Directivo la confirme.</p>`
    );
  }
  if (paso === "espera") {
    const pagina = ctx.estado.pagina;
    const lista = await ctx.api(
      `${rutaInstitucional(ctx)}/lista-espera?pagina=${pagina}&tamanoPagina=20`,
    );
    ctx.organizacion = { lista };
    const filas = (xs) =>
      tabla(
        ["Aspirante", "Sala", "Posición general / espera", "Estado", "Motivo"],
        xs.map((x) => [
          enlaceSolicitud(ctx, x.solicitudId),
          e(x.sala ?? "Pendiente"),
          e(`${x.posicionGeneral ?? "—"} / ${x.posicionEspera ?? "—"}`),
          etiqueta(x.estado),
          e(x.motivo),
        ]),
      );
    return (
      cabecera +
      resumen(
        "Lista de espera",
        `<p>${e(lista.estadoProceso)} · ${lista.totalEnEspera} en espera · ${lista.totalPendientes} pendientes</p>${(lista.avisos ?? []).map((x) => `<p>${e(x)}</p>`).join("")}` +
          filas(lista.enEspera.resultados),
      ) +
      resumen("Casos pendientes", filas(lista.pendientes.resultados)) +
      `<div class="formulario__acciones">${boton("pagina-anterior", "Anterior", "", pagina <= 1)}<span>Página ${pagina}</span>${boton("pagina-siguiente", "Siguiente", "", !lista.enEspera.hayMas && !lista.pendientes.hayMas)}</div>`
    );
  }
  if (paso === "ofrecimientos") {
    const ofrecimientos = await ctx.api(`${p}/ofrecimientos`);
    ctx.organizacion = { ofrecimientos };
    return (
      cabecera +
      resumen(
        "Cubrir una vacante",
        "<p>Consultá los candidatos compatibles antes de ofrecer. La reserva evita ocupar dos veces una plaza; rechazar un turno alternativo conserva la espera.</p>",
        boton("org-candidatos", "Consultar candidatos"),
      ) +
      resumen(
        "Ofrecimientos",
        tabla(
          ["Aspirante", "Turno", "Estado", "Fecha", "Acciones"],
          ofrecimientos.map((x) => [
            enlaceSolicitud(ctx, x.solicitudId),
            e(nombreTurno(ctx, x.turnoId)),
            etiqueta(x.estado),
            e(fecha(x.fecha)),
            boton("org-ofrecimiento-historial", "Historial", x.id) +
              (["Ofrecido", "Aceptado"].includes(x.estado)
                ? boton("org-respuesta", "Registrar respuesta", x.id)
                : "") +
              (x.estado === "Aceptado" && ctx.puedeResolver()
                ? boton("org-confirmar-ofrecimiento", "Confirmar ingreso", x.id)
                : ""),
          ]),
        ),
      )
    );
  }
  if (paso === "ingresos") {
    const reporte = await ctx.api(`${rutaInstitucional(ctx)}/reportes`);
    ctx.organizacion = { ingresos: reporte.ingresos };
    return (
      cabecera +
      resumen(
        "Ingresos confirmados e históricos",
        tabla(
          ["Aspirante", "Sala", "Turno", "Estado", "Acciones"],
          reporte.ingresos.map((x) => [
            enlaceSolicitud(ctx, x.solicitudId),
            e(x.sala),
            e(nombreTurno(ctx, x.turnoId)),
            etiqueta(
              x.bajaEnProceso ? "Baja pendiente" : x.activo ? "Activo" : "Baja",
            ),
            boton("org-baja-historial", "Consultar baja", x.id) +
              (x.activo && ctx.puedeResolver()
                ? boton(
                    "org-baja",
                    x.bajaEnProceso ? "Reintentar baja" : "Registrar baja",
                    x.id,
                  )
                : ""),
          ]),
        ),
      )
    );
  }
  const [propuestas, estadoProcesamiento] = await Promise.all([
    ctx.api(`${p}/propuestas-ingresos`),
    ctx.api(`${p}/prioridades/estado`),
  ]);
  ctx.organizacion = { propuestas, estadoProcesamiento };
  const puedeGenerar = ctx.puedeResolver() && estadoProcesamiento.recepcionCerrada && estadoProcesamiento.procesado && estadoProcesamiento.vigente && estadoProcesamiento.pendientesRevision === 0;
  return (
    cabecera +
    resumen(
      "Propuestas de ingreso",
      tabla(
        ["Fecha", "Estado", "Revisión", "Motivo", "Acción"],
        propuestas.map((x) => [
          e(fecha(x.fecha)),
          etiqueta(x.estado),
          e(x.revision),
          e(x.motivo),
          `<a class="boton boton--secundario boton--pequeno" href="#vacantes/propuesta/${encodeURIComponent(x.id)}">Revisar propuesta</a>`,
        ]),
      ),
      boton("org-generar-propuesta", "Generar propuesta", "", !puedeGenerar),
    ) +
    `<p class="formulario__nota">${puedeGenerar ? "La lista ya fue procesada. Generá la propuesta para revisarla antes de confirmarla." : estadoProcesamiento.pendientesRevision > 0 ? `Resolvé las ${estadoProcesamiento.pendientesRevision} prioridades pendientes y procesá la lista antes de generar una propuesta.` : !estadoProcesamiento.recepcionCerrada ? "Cerrá la recepción antes de procesar la lista de aspirantes." : "Procesá la lista desde la pestaña Prioridades antes de generar una propuesta."} Los conflictos relevantes no se resuelven automáticamente.</p>`
  );
}
