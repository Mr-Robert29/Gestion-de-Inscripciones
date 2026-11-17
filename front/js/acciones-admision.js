import { escapar as e, campo, fecha } from "./interfaz.js";
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
    const participa = (x) => clave === "FueraDeTermino"
      ? x.explicacion?.includes("Solicitud fuera de término.")
      : clave === "SorteoUltimaInstancia"
        ? x.requiereRevision
        : x.explicacion?.some((texto) =>
            texto === `Criterio verificado: ${clave}.` ||
            (clave === "Cercania" && texto.startsWith("Cercanía:")),
          );
    const listado = (items) => tabla(
      ["Aspirante", "Sala", "Posición", "Estado de evaluación"],
      items.map((x) => [
        `<a href="#solicitud/${encodeURIComponent(x.solicitudId)}">${e(nombreSolicitud(ctx, x.solicitudId))}</a>`,
        e(x.sala ?? "Pendiente"),
        e(x.posicion ?? "Pendiente"),
        e(x.motivoRevision ?? (x.requiereRevision ? "Requiere revisión" : "Evaluado")),
      ]),
    );
    const incluidos = filas.filter(participa);
    const siguientes = filas.filter((x) => !participa(x));
    const criteriosEtapa = ["ArticulacionPedagogica", "HermanosYGruposConvivientes", "Cercania", "HijosDelPersonal"];
    const etapasOrdenadas = [...criteriosEtapa, "SorteoUltimaInstancia", "FueraDeTermino"];
    const etapaPrioridad = (prioridad) => criteriosEtapa.find((key) => prioridad.explicacion?.some((text) => text === `Criterio verificado: ${key}.` || (key === "Cercania" && text.startsWith("Cercanía:")))) ?? (prioridad.explicacion?.includes("Solicitud fuera de término.") ? "FueraDeTermino" : null);
    const indiceEtapa = etapasOrdenadas.indexOf(clave);
    const capacidadInicial = (ctx.estado.ofertasPublicadas ?? []).reduce(
      (total, oferta) => total + Number(oferta.capacidadNetaInicial ?? oferta.capacidad ?? 0), 0,
    );
    const resultadosPropuesta = datos.propuestaVigente?.resultados ?? [];
    const propuestasAcumuladas = resultadosPropuesta.filter((resultado) => {
      if (resultado.estado !== "IngresoPropuesto") return false;
      const prioridad = filas.find((x) => x.solicitudId === resultado.solicitudId);
      const etapa = prioridad ? etapaPrioridad(prioridad) : null;
      return etapa && etapasOrdenadas.indexOf(etapa) <= indiceEtapa;
    }).length;
    const propuestasDeEtapa = resultadosPropuesta.filter((resultado) => resultado.estado === "IngresoPropuesto" && filas.find((x) => x.solicitudId === resultado.solicitudId && etapaPrioridad(x) === clave)).length;
    const quedan = Math.max(0, capacidadInicial - propuestasAcumuladas);
    const resumenCupos = `<div class="prioridades__metricas"><div><span>Cupos publicados</span><strong>${capacidadInicial}</strong></div><div><span>Propuestas en esta etapa</span><strong>${propuestasDeEtapa}</strong></div><div><span>Cupos por distribuir</span><strong>${datos.propuestaVigente ? quedan : "—"}</strong></div></div>`;
    ver(
      ctx,
      nombres[clave],
      resumenCupos +
        `<p class="formulario__nota">${datos.propuestaVigente ? `Cálculo basado en la propuesta vigente del ${e(datos.propuestaVigente.fecha)}` : "Todavía no hay una propuesta vigente para calcular el impacto de los cupos."} · Las propuestas no son ingresos confirmados; los cupos se ocupan definitivamente al confirmar los ingresos.</p>` +
        (clave === "SorteoUltimaInstancia"
          ? `<p>${(datos.sorteos ?? []).length} sorteos registrados para este período.</p>`
          : `<section class="prioridades__grupo"><h3>Con prioridad verificada · ${incluidos.length}</h3>${listado(incluidos)}</section><section class="prioridades__grupo"><h3>Sin prioridad acreditada en este criterio · ${siguientes.length}</h3>${listado(siguientes)}</section>`),
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
        referencia(),
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
