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
  if (accion === "org-generar-propuesta")
    formulario(ctx, "Generar propuesta de ingresos", motivo(), async (d) => {
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
        motivo(),
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
      formulario(ctx, "Reabrir caso", motivo(), (d) =>
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
          motivo(),
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
