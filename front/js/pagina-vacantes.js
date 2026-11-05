import { escapar as e, etiqueta, fecha, seleccion } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
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
  ["vacantes/prioridades", "Prioridades y sorteo"],
  ["vacantes/espera", "Lista de espera"],
  ["vacantes/ofrecimientos", "Ofrecimientos"],
  ["vacantes/ingresos", "Ingresos y bajas"],
];
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
    const [prioridades, sorteos, casos] = await Promise.all([
      ctx.api(`${p}/prioridades`),
      ctx.api(`${p}/sorteos`),
      ctx.api(`${p}/casos-revision`).catch((error) => {
        if (error.estado !== 404) throw error;
        return { aviso: error.message };
      }),
    ]);
    ctx.organizacion = {
      prioridades,
      sorteos,
      casos: Array.isArray(casos) ? casos : [],
    };
    return (
      cabecera +
      resumen(
        "Orden y criterios aplicados",
        tabla(
          ["Aspirante", "Sala", "Posición", "Criterios / revisión"],
          prioridades.map((x) => [
            enlaceSolicitud(ctx, x.solicitudId),
            e(x.sala ?? "Pendiente"),
            e(x.posicion ?? "—"),
            e(
              [
                ...(x.criteriosAplicados ?? []),
                x.motivoRevision,
                ...(x.explicacion ?? []),
              ]
                .filter(Boolean)
                .join(" · "),
            ),
          ]),
        ),
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
    ctx.organizacion = { propuesta };
    return (
      cabecera +
      resumen(
        `Propuesta · ${fecha(propuesta.fecha)}`,
        `<p>${etiqueta(propuesta.estado)} · ${propuesta.entradasVigentes ? "Datos vigentes" : "Requiere revisión"}</p><p>${e(propuesta.motivo)}</p><div class="formulario__acciones">${boton("org-confirmar-propuesta", "Confirmar ingresos", propuesta.id, !ctx.puedeResolver() || propuesta.estado === "Confirmada" || !propuesta.entradasVigentes)}</div>` +
          tabla(
            [
              "Aspirante",
              "Sala",
              "Resultado",
              "Turno",
              "Explicación",
              "Acciones",
            ],
            propuesta.resultados.map((x) => [
              enlaceSolicitud(ctx, x.solicitudId),
              e(x.sala ?? "Pendiente"),
              etiqueta(x.estado),
              e(nombreTurno(ctx, x.turnoId)),
              e(x.explicacion),
              (ctx.puedeResolver() && propuesta.estado !== "Confirmada"
                ? boton(
                    "org-modificar-propuesta",
                    "Revisar resultado",
                    x.solicitudId,
                  )
                : "") +
                (x.esTurnoAlternativo &&
                x.estado === "IngresoPropuesto" &&
                propuesta.estado !== "Confirmada"
                  ? boton(
                      "org-aceptacion",
                      "Registrar aceptación familiar",
                      x.solicitudId,
                    )
                  : ""),
            ]),
          ),
      ) +
      '<p class="formulario__nota">Generar o revisar no confirma ingresos. Cada alternativa de turno exige aceptación familiar registrada.</p>'
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
  const propuestas = await ctx.api(`${p}/propuestas-ingresos`);
  ctx.organizacion = { propuestas };
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
      boton("org-generar-propuesta", "Generar propuesta"),
    ) +
    '<p class="formulario__nota">Cerrá la recepción y revisá documentación, distancias y casos pendientes antes de generar. Los conflictos relevantes no se resuelven automáticamente.</p>'
  );
}
