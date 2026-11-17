import { escapar as e, campo, fecha } from "./interfaz.js";
import { candidatosDeEtapa, cohortePrioridad, cohortesIncluidasHasta, cumpleCriterio, prioridadVerificadaEnEtapa } from "./prioridades-etapas.js";
import {
  rutaPeriodo,
  rutaInstitucional,
  nombreTurno,
  nombreAula,
  nombreSolicitud,
  tabla,
  historial,
} from "./organizacion-comun.js";
import {
  motivo,
  motivos,
  referencia,
  selector,
  opcionesEdades,
  enviar,
  formulario,
  ver,
  exigirRegistro,
} from "./operaciones-formularios.js";

export async function atenderAdmision(elemento, ctx) {
  const accion = elemento.dataset.accion;
  const id = elemento.dataset.id;
  const p = ctx.estado.periodo ? rutaPeriodo(ctx) : null;
  const datos = ctx.organizacion ?? {};
  if (accion === "org-procesar-prioridades")
    formulario(
      ctx,
      "Procesar lista de aspirantes",
      '<p>El sistema aplicará las prioridades en el orden configurado. Este paso calcula el orden; todavía no asigna ni confirma vacantes.</p><label class="casilla"><input type="checkbox" required>Confirmo que revisé las prioridades pendientes y que la lista está lista para procesarse.</label>',
      async () => {
        await enviar(ctx, `${p}/prioridades/procesamiento`, {});
        return {
          destino: "vacantes",
          mensaje: "La lista se procesó. Ya podés generar y revisar la propuesta de ingreso.",
        };
      },
      { textoConfirmar: "Procesar lista" },
    );
  else if (accion === "org-generar-propuesta")
    formulario(ctx, "Generar propuesta de ingresos", motivo(motivos.propuestaAdmision), async (d) => {
      const x = await enviar(ctx, `${p}/propuestas-ingresos`, d);
      return {
        destino: `vacantes/propuesta/${x.id}`,
        mensaje: "Propuesta generada. Revisala antes de confirmar.",
      };
    });
  else if (accion === "org-confirmar-propuesta") {
    const x = datos.propuesta;
    formulario(
      ctx,
      "Confirmar ingresos",
      '<p>Se registrarán los ingresos definitivos de esta propuesta. El servidor volverá a comprobar disponibilidad, vigencia y aceptaciones.</p><label class="casilla"><input type="checkbox" required>Revisé la propuesta y las aceptaciones familiares necesarias.</label>',
      async () => {
        await enviar(ctx, `/propuestas-ingresos/${x.id}/confirmacion`, {
          huellaEsperada: x.huellaEntradas,
        });
        return { destino: "vacantes/ingresos" };
      },
    );
  } else if (accion === "org-ver-resultado-propuesta") {
    const propuesta = datos.propuesta;
    const resultado = propuesta?.resultados.find((x) => x.solicitudId === id);
    if (!resultado)
      throw new Error("No encontramos el resultado de esta solicitud en la propuesta.");
    const prioridad = datos.prioridades?.find((x) => x.solicitudId === id);
    const oferta = (ctx.estado.ofertasPublicadas ?? ctx.estado.ofertas ?? []).find(
      (x) => x.id === resultado.ofertaId,
    );
    const estadoLegible = {
      IngresoPropuesto: "Ingreso propuesto",
      EsperaPropuesta: "En lista de espera",
      PendienteRevision: "Requiere revisión",
      YaOcupaPlaza: "Ya tiene un ingreso",
    }[resultado.estado] ?? resultado.estado;
    const datosPrioridad = prioridad?.explicacion?.length
      ? `<ul>${prioridad.explicacion.map((x) => `<li>${e(x)}</li>`).join("")}</ul>`
      : '<p class="formulario__nota">La propuesta no tiene una evaluación de prioridad vinculada para este aspirante.</p>';
    const prioridadLegible = prioridad
      ? prioridad.explicacion?.find((x) => x.startsWith("Cercanía:"))?.replace("Cercanía:", "Cercanía ·")
        ?? prioridad.explicacion?.find((x) => x.startsWith("Criterio verificado:"))?.replace("Criterio verificado: ", "").replace(".", "")
        ?? "Sin prioridad acreditada"
      : "Sin evaluación vinculada";
    ver(
      ctx,
      `Detalle de propuesta · ${nombreSolicitud(ctx, id)}`,
      `<dl class="propuesta__detalle"><div><dt>Resultado</dt><dd>${e(estadoLegible)}</dd></div><div><dt>Grupo etario</dt><dd>${e(resultado.sala ? `Sala de ${resultado.sala} años` : "Pendiente")}</dd></div><div><dt>Oferta</dt><dd>${e(oferta ? `${nombreAula(ctx, oferta.aulaId)} · ${nombreTurno(ctx, oferta.turnoId)}` : "Sin oferta asignada")}${resultado.esTurnoAlternativo ? " · turno alternativo" : ""}</dd></div><div><dt>Prioridad considerada</dt><dd>${e(prioridadLegible)}</dd></div></dl><section class="propuesta__detalle-explicacion"><h3>Cómo se evaluó</h3>${datosPrioridad}<p><strong>Explicación de la propuesta:</strong> ${e(resultado.explicacion ?? "Sin explicación adicional.")}</p></section><p class="formulario__nota">La propuesta es preliminar. El ingreso queda confirmado cuando el Directivo confirma la propuesta completa.</p>`,
    );
  } else if (accion === "org-revisar-articulacion") {
    const articulacion = datos.articulaciones?.find(
      (x) => x.condicion.id === id,
    );
    if (!articulacion)
      throw new Error("La declaración cambió. Actualizá la pantalla e intentá de nuevo.");
    const esArticulacion = ["ArticulacionPedagogica", "ConsultaArticulacionPedagogica"].includes(articulacion.condicion.tipo);
    const categoria = esArticulacion ? "articulación pedagógica" : articulacion.condicion.tipo === "HermanosYGruposConvivientes" ? "hermanos o grupo conviviente" : "hijo/a del personal";
    const opcionesEstado = [
      { id: "", nombre: "Seleccioná una decisión" },
      { id: "Verificada", nombre: "Verificada: corresponde aplicar la prioridad" },
      { id: "Observada", nombre: "Observada: requiere una aclaración" },
    ];
    if (ctx.estado.periodo.estado === "RecepcionCerrada")
      opcionesEstado.push({ id: "NoAcreditada", nombre: "No acreditada al cierre: no se aplica esta prioridad" });
    formulario(
      ctx,
      `Revisar ${categoria}`,
      `<p><strong>${e(nombreSolicitud(ctx, articulacion.solicitudId))}</strong></p><p>${e(articulacion.condicion.valorDeclarado)}</p><p><strong>Dato informado:</strong> ${e(articulacion.condicion.evidenciaReferencia ?? "No se informó un detalle")}</p><p class="formulario__nota">“Verificada” confirma que la declaración corresponde a este aspirante. “Observada” deja el caso pendiente de aclaración. “No acreditada al cierre” registra que no se pudo comprobar antes del cierre.</p>` +
        selector("estado", "Decisión", opcionesEstado) +
        campo({ nombre: "observacion", etiqueta: "Aclaración (opcional)", ayuda: "Anotá un dato útil para el seguimiento, como qué falta revisar o por qué no corresponde." }) +
        motivo([
          `Se verificó la prioridad de ${categoria}`,
          `La declaración de ${categoria} no corresponde a este aspirante`,
          "Falta información para confirmar la declaración",
          "El dato informado no pudo ser corroborado",
          "No se acreditó la prioridad antes del cierre",
        ], { etiqueta: "Motivo de la decisión" }),
      (d) => {
        return enviar(
          ctx,
          `/instituciones/${encodeURIComponent(ctx.estado.institucion.id)}/solicitudes/${articulacion.solicitudId}/condiciones/${articulacion.condicion.id}`,
          {
            estado: d.estado,
            revisionSolicitud: articulacion.revisionSolicitud,
            motivo: d.motivo,
            evidenciaReferencia: null,
            observacion: d.observacion || null,
          },
          "PUT",
        );
      },
    );
  } else if (accion === "org-confirmar-prioridades-lote") {
    const grupos = {
      articulacion: {
        titulo: "articulaciones pedagógicas",
        tituloSingular: "articulación pedagógica",
        tipos: ["ArticulacionPedagogica", "ConsultaArticulacionPedagogica"],
        motivo: "Se verificó la articulación pedagógica declarada",
      },
      hermanos: {
        titulo: "hermanos o grupos convivientes",
        tituloSingular: "hermano o grupo conviviente",
        tipos: ["HermanosYGruposConvivientes"],
        motivo: "Se verificó el vínculo de hermano o grupo conviviente declarado",
      },
      personal: {
        titulo: "hijos del personal",
        tituloSingular: "hijo/a del personal",
        tipos: ["HijosDelPersonal"],
        motivo: "Se verificó la relación laboral declarada",
      },
    };
    const grupo = grupos[id];
    if (!grupo) throw new Error("No se reconoció el grupo de prioridades.");
    const candidatos = datos.articulaciones?.filter(
      (x) => grupo.tipos.includes(x.condicion.tipo) && x.condicion.estadoVerificacion === "PendienteRevision",
    ) ?? [];
    if (!candidatos.length) throw new Error("No hay casos pendientes para confirmar en este grupo.");
    const listado = candidatos.map((x) =>
      `<li><strong>${e(nombreSolicitud(ctx, x.solicitudId))}</strong><br><span>${e(x.condicion.valorDeclarado)}</span><br><small>${e(x.condicion.evidenciaReferencia ?? "Sin detalle informado")}</small></li>`,
    ).join("");
    formulario(
      ctx,
      candidatos.length === 1
        ? `Confirmar 1 caso de ${grupo.tituloSingular}`
        : `Confirmar ${candidatos.length} casos de ${grupo.tituloSingular}`,
      `<p>La acción marcará todos los casos pendientes de esta lista como verificados. Cada decisión quedará registrada en el historial individual de la solicitud.</p><ul class="prioridades__lote-lista">${listado}</ul><label class="casilla"><input type="checkbox" name="confirmarTodos" required>Revisé los datos de todos los casos listados y confirmo que corresponde aplicar esta prioridad.</label>${motivo([grupo.motivo, "Se verificó la información declarada por la familia", "Se revisó la documentación institucional disponible"], { etiqueta: "Motivo de la confirmación" })}`,
      async (d) => {
        if (!d.confirmarTodos) throw new Error("Confirmá que revisaste todos los casos de la lista.");
        let confirmadas = 0;
        let omitidas = 0;
        const fallidas = [];
        for (const candidato of candidatos) {
          const rutaSolicitud = `/instituciones/${encodeURIComponent(ctx.estado.institucion.id)}/solicitudes/${encodeURIComponent(candidato.solicitudId)}`;
          try {
            const antecedentes = await ctx.api(`${rutaSolicitud}/antecedentes`);
            const condicionActual = antecedentes.condiciones?.find((x) => x.id === candidato.condicion.id);
            if (!condicionActual || !grupo.tipos.includes(condicionActual.tipo) || condicionActual.estadoVerificacion !== "PendienteRevision") {
              omitidas++;
              continue;
            }
            await enviar(
              ctx,
              `${rutaSolicitud}/condiciones/${encodeURIComponent(condicionActual.id)}`,
              {
                estado: "Verificada",
                revisionSolicitud: antecedentes.revisionSolicitud,
                motivo: d.motivo,
                evidenciaReferencia: null,
                observacion: null,
              },
              "PUT",
            );
            confirmadas++;
          } catch (error) {
            fallidas.push(nombreSolicitud(ctx, candidato.solicitudId));
            if (error.estado !== 409) break;
          }
        }
        let mensaje = `Se confirmaron ${confirmadas} de ${candidatos.length} casos de ${grupo.titulo}.`;
        if (omitidas) mensaje += ` ${omitidas} ya habían cambiado y se omitieron.`;
        if (fallidas.length) mensaje += ` No se pudieron revisar ${fallidas.length}; actualizá la lista y continuá individualmente.`;
        return { mensaje };
      },
    );
  } else if (accion === "org-ver-etapa") {
    const nombres = {
      ArticulacionPedagogica: "1. Articulación pedagógica",
      HermanosYGruposConvivientes: "2. Hermanos o grupos convivientes",
      Cercania: "3. Cercanía al jardín",
      HijosDelPersonal: "4. Hijos del personal",
      SorteoUltimaInstancia: "5. Sorteo, última instancia",
      FueraDeTermino: "6. Inscripciones fuera de término",
    };
    const clave = id;
    if (!nombres[clave]) throw new Error("No se encontró la etapa de prioridad.");
    const filas = datos.prioridades ?? [];
    const listado = (items) => tabla(
      ["Aspirante", "Sala", "Posición", "Estado de evaluación"],
      items.map((x) => [
        `<a href="#solicitud/${encodeURIComponent(x.solicitudId)}">${e(nombreSolicitud(ctx, x.solicitudId))}</a>`,
        e(x.sala ?? "Pendiente"),
        e(x.posicion ?? "Pendiente"),
        e(x.motivoRevision ?? (x.requiereRevision ? "Requiere revisión" : "Evaluado")),
      ]),
    );
    const candidatos = candidatosDeEtapa(filas, clave);
    const incluidos = clave === "SorteoUltimaInstancia"
      ? candidatos
      : clave === "FueraDeTermino"
        ? candidatos
        : clave === "Cercania"
          ? candidatos
          : candidatos.filter((x) => prioridadVerificadaEnEtapa(x, clave));
    const siguientes = ["ArticulacionPedagogica", "HermanosYGruposConvivientes"].includes(clave)
      ? candidatos.filter((x) => !prioridadVerificadaEnEtapa(x, clave))
      : [];
    const capacidadInicial = (ctx.estado.ofertasPublicadas ?? []).reduce(
      (total, oferta) => total + Number(oferta.capacidadNetaInicial ?? oferta.capacidad ?? 0), 0,
    );
    const resultadosPropuesta = datos.propuestaVigente?.resultados ?? [];
    const prioridadPorSolicitud = new Map(filas.map((x) => [x.solicitudId, x]));
    const cohorteDeResultado = (resultado) => {
      const prioridad = prioridadPorSolicitud.get(resultado.solicitudId);
      return prioridad ? cohortePrioridad(prioridad) : null;
    };
    const perteneceEtapa = (resultado) => {
      const prioridad = prioridadPorSolicitud.get(resultado.solicitudId);
      if (!prioridad || resultado.estado !== "IngresoPropuesto") return false;
      if (clave === "HijosDelPersonal")
        return cohorteDeResultado(resultado) === "Cercania" && cumpleCriterio(prioridad, "HijosDelPersonal");
      if (clave === "SorteoUltimaInstancia") return prioridad.requiereRevision;
      if (clave === "FueraDeTermino") return prioridad.explicacion?.includes("Solicitud fuera de término.");
      return cohorteDeResultado(resultado) === clave;
    };
    const etapasIncluidas = cohortesIncluidasHasta(clave);
    const propuestasAcumuladas = resultadosPropuesta.filter((resultado) => {
      if (resultado.estado !== "IngresoPropuesto") return false;
      if (clave === "SorteoUltimaInstancia" || clave === "FueraDeTermino") return false;
      return etapasIncluidas.includes(cohorteDeResultado(resultado));
    }).length;
    const propuestasDeEtapa = resultadosPropuesta.filter(perteneceEtapa).length;
    const quedan = Math.max(0, capacidadInicial - propuestasAcumuladas);
    const tablaCupos = tabla(
      ["Oferta (aula · turno)", "Cupos publicados", "Propuestas en esta etapa", "Propuestas acumuladas", "Cupos restantes"],
      (ctx.estado.ofertasPublicadas ?? []).map((oferta) => {
        const capacidad = Number(oferta.capacidadNetaInicial ?? oferta.capacidad ?? 0);
        const enEtapa = resultadosPropuesta.filter((resultado) => perteneceEtapa(resultado) && resultado.ofertaId === oferta.id).length;
        const acumuladas = resultadosPropuesta.filter((resultado) => {
          if (resultado.estado !== "IngresoPropuesto" || resultado.ofertaId !== oferta.id) return false;
          return clave !== "SorteoUltimaInstancia" && clave !== "FueraDeTermino" && etapasIncluidas.includes(cohorteDeResultado(resultado));
        }).length;
        return [
          e(`${nombreAula(ctx, oferta.aulaId)} · ${nombreTurno(ctx, oferta.turnoId)}`),
          e(capacidad),
          e(enEtapa),
          e(acumuladas),
          e(Math.max(0, capacidad - acumuladas)),
        ];
      }),
    );
    const resumenCupos = `<div class="prioridades__metricas"><div><span>Cupos publicados en el período</span><strong>${capacidadInicial}</strong></div><div><span>Propuestas en esta etapa</span><strong>${propuestasDeEtapa}</strong></div><div><span>Cupos restantes después de la etapa</span><strong>${quedan}</strong></div></div>`;
    const descripcionEtapa = clave === "Cercania"
      ? "A esta etapa llegan únicamente quienes no obtuvieron prioridad por articulación ni por hermanos o grupo conviviente. Se ordenan por distancia; no es una pregunta de sí o no."
      : clave === "HijosDelPersonal"
        ? "Este criterio se aplica después de la cercanía y funciona como desempate dentro del grupo restante. No crea una segunda tanda de aspirantes ni vuelve a consumir los cupos ya propuestos."
        : clave === "ArticulacionPedagogica"
          ? "Se revisa primero a toda la lista. Quienes tienen articulación verificada quedan en esta cohorte; los demás continúan a la etapa de hermanos."
          : clave === "HermanosYGruposConvivientes"
            ? "A esta etapa llegan solo quienes no tienen articulación verificada. Quienes acreditan hermanos o grupo conviviente forman la cohorte prioritaria; el resto continúa a cercanía."
            : "El detalle muestra los casos correspondientes a esta etapa del proceso.";
    ver(
      ctx,
      nombres[clave],
      `<p>${descripcionEtapa}</p>` +
        resumenCupos +
        `<p class="formulario__nota">${datos.propuestaVigente ? `Saldo calculado con la propuesta vigente del ${e(fecha(datos.propuestaVigente.fecha))}.` : "Todavía no se generó una propuesta: los cupos siguen publicados, pero aún no están distribuidos entre las etapas."} La propuesta no confirma ingresos; los cupos se ocupan definitivamente al confirmar los ingresos.</p>` +
        `<section class="prioridades__grupo"><h3>Impacto por oferta de aula y turno</h3>${tablaCupos}</section>` +
        (clave === "SorteoUltimaInstancia"
          ? `<p>${(datos.sorteos ?? []).length} sorteos registrados para este período.</p>${listado(incluidos)}`
          : clave === "Cercania"
            ? `<section class="prioridades__grupo"><h3>Solicitudes que llegan a cercanía · ${candidatos.length}</h3>${listado(candidatos)}</section>`
            : clave === "HijosDelPersonal"
              ? `<section class="prioridades__grupo"><h3>Hijos del personal dentro del grupo restante · ${incluidos.length}</h3>${listado(incluidos)}</section>`
              : clave === "FueraDeTermino"
                ? `<section class="prioridades__grupo"><h3>Solicitudes fuera de término · ${incluidos.length}</h3>${listado(incluidos)}</section>`
                : `<section class="prioridades__grupo"><h3>Con prioridad verificada en esta etapa · ${incluidos.length}</h3>${listado(incluidos)}</section><section class="prioridades__grupo"><h3>Continúan a la etapa siguiente · ${siguientes.length}</h3>${listado(siguientes)}</section>`),
    );
  } else if (accion === "org-modificar-propuesta") {
    const propuesta = datos.propuesta;
    const x = propuesta.resultados.find((x) => x.solicitudId === id);
    const ofertas = ctx.estado.ofertas
      .filter((x) => x.disponible)
      .map((x) => ({
        id: x.id,
        nombre: `${nombreAula(ctx, x.aulaId)} · ${nombreTurno(ctx, x.turnoId)}`,
      }));
    formulario(
      ctx,
      `Revisar ${nombreSolicitud(ctx, id)}`,
      selector(
        "estado",
        "Resultado",
        [
          { id: "IngresoPropuesto", nombre: "Proponer ingreso" },
          { id: "EsperaPropuesta", nombre: "Mantener en espera" },
        ],
        x.estado,
      ) +
        selector(
          "ofertaId",
          "Oferta para ingreso (se ignora para espera)",
          [{ id: "", nombre: "Sin oferta" }, ...ofertas],
          x.ofertaId ?? "",
          false,
        ) +
        '<label class="casilla"><input type="checkbox" name="alternativo">El turno elegido es una alternativa aceptada por la familia.</label>' +
        motivo(motivos.revisarPropuesta),
      async (d) => {
        const oferta = ctx.estado.ofertas.find((o) => o.id === d.ofertaId);
        if (d.estado === "IngresoPropuesto" && !oferta)
          throw new Error("Elegí una oferta para el ingreso.");
        const ingreso = d.estado === "IngresoPropuesto";
        const revision = await enviar(
          ctx,
          `/propuestas-ingresos/${propuesta.id}`,
          {
            huellaEsperada: propuesta.huellaEntradas,
            motivo: d.motivo,
            cambios: [
              {
                solicitudId: id,
                estado: d.estado,
                ofertaId: ingreso ? oferta.id : null,
                turnoId: ingreso ? oferta.turnoId : null,
                esTurnoAlternativo: ingreso && !!d.alternativo,
                explicacion: d.motivo,
              },
            ],
          },
          "PATCH",
        );
        return { destino: `vacantes/propuesta/${revision.id}` };
      },
    );
  } else if (accion === "org-aceptacion") {
    const x = datos.propuesta.resultados.find((x) => x.solicitudId === id);
    formulario(
      ctx,
      "Registrar aceptación familiar del turno",
      `<p>${e(nombreSolicitud(ctx, id))} · ${e(nombreTurno(ctx, x.turnoId))}</p>` +
        campo({
          nombre: "referencia",
          etiqueta: "Constancia de aceptación",
          obligatorio: true,
          ayuda: "Anotá qué constancia presentó la familia. Ejemplo: Nota de aceptación del turno tarde, recibida el 17/11/2026.",
          atributos: 'placeholder="Ej.: nota firmada y fecha de recepción" maxlength="240"',
        }),
      (d) =>
        enviar(
          ctx,
          `/propuestas-ingresos/${datos.propuesta.id}/aceptaciones-turno`,
          { solicitudId: id, turnoId: x.turnoId, referencia: d.referencia },
        ),
    );
  } else if (accion === "org-sorteo")
    formulario(
      ctx,
      "Registrar sorteo de última instancia",
      selector("sala", "Grupo etario", opcionesEdades) +
        campo({
          nombre: "numero",
          etiqueta: "Número inicial (000 a 999)",
          tipo: "number",
          obligatorio: true,
          atributos: 'min="0" max="999" step="1"',
        }) +
        campo({
          nombre: "fecha",
          etiqueta: "Fecha del sorteo",
          tipo: "date",
          obligatorio: true,
        }) +
        referencia(),
      (d) =>
        enviar(ctx, `${p}/sorteos`, {
          ...d,
          sala: Number(d.sala),
          numero: Number(d.numero),
        }),
    );
  else if (accion === "org-orden") {
    const orden = await ctx.api(`${p}/sorteos/${id}/orden`);
    ver(
      ctx,
      "Orden del grupo participante",
      tabla(
        ["Solicitud", "Terminación", "Posición", "Revisión"],
        orden.map((x) => [
          e(nombreSolicitud(ctx, x.solicitudId)),
          e(x.terminacionDni ?? "Sin DNI"),
          e(x.posicion ?? "Pendiente"),
          e(x.motivoRevision ?? "—"),
        ]),
      ),
    );
  } else if (
    ["org-resolver", "org-reabrir", "org-caso-historial"].includes(accion)
  ) {
    const x = exigirRegistro(datos.casos, id);
    if (accion === "org-caso-historial")
      ver(
        ctx,
        "Historial del caso",
        historial(
          "Resoluciones conservadas",
          await ctx.api(`/casos-revision/${id}/historial`),
        ),
      );
    else if (accion === "org-reabrir")
      formulario(ctx, "Reabrir caso", motivo(motivos.reabrirCaso), (d) =>
        enviar(ctx, `/casos-revision/${id}/reabrir`, {
          motivo: d.motivo,
          revision: x.revision,
        }),
      );
    else
      formulario(
        ctx,
        "Registrar orden decidido manualmente",
        "<p>Asigná una posición distinta a cada participante. Ninguna posición se decide automáticamente.</p>" +
          x.solicitudesAfectadas
            .map((s, i) =>
              selector(`posicion${i}`, nombreSolicitud(ctx, s), [
                { id: "", nombre: "Elegí una posición" },
                ...x.solicitudesAfectadas.map((_, n) => ({
                  id: String(n + 1),
                  nombre: String(n + 1),
                })),
              ]),
            )
            .join("") +
          motivo(motivos.ordenManual),
        (d) => {
          const posiciones = x.solicitudesAfectadas.map((s, i) => ({
            solicitud: s,
            posicion: Number(d[`posicion${i}`]),
          }));
          if (
            new Set(posiciones.map((x) => x.posicion)).size !==
            posiciones.length
          )
            throw new Error(
              "Cada participante debe tener una posición diferente.",
            );
          return enviar(ctx, `/casos-revision/${id}/resolver`, {
            revision: x.revision,
            motivo: d.motivo,
            ordenDecidido: posiciones
              .sort((a, b) => a.posicion - b.posicion)
              .map((x) => x.solicitud),
          });
        },
      );
  }
  return true;
}
