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

export async function atenderEspera(elemento, ctx) {
  const accion = elemento.dataset.accion;
  const id = elemento.dataset.id;
  const p = ctx.estado.periodo ? rutaPeriodo(ctx) : null;
  const datos = ctx.organizacion ?? {};
  if (accion === "org-candidatos") {
    const opciones = ctx.estado.ofertas
      .filter((x) => x.disponible)
      .map((x) => ({
        id: x.id,
        nombre: `${nombreAula(ctx, x.aulaId)} · ${nombreTurno(ctx, x.turnoId)}`,
      }));
    formulario(
      ctx,
      "Consultar candidatos compatibles",
      selector("ofertaId", "Aula y turno", opciones),
      async (d) => {
        const lista = await ctx.api(`${p}/ofertas/${d.ofertaId}/candidatos`);
        // Este paso solo consulta; el ofrecimiento se confirma en un formulario posterior.
        ctx.candidatosOferta = { ...lista, operacionId: crypto.randomUUID() };
        return {
          destino: `vacantes/candidatos/${d.ofertaId}`,
          mensaje: "Candidatos consultados.",
        };
      },
    );
  } else if (accion === "org-ofrecer") {
    const lista = ctx.candidatosOferta;
    if (!lista || lista.periodoId !== ctx.estado.periodo.id)
      throw new Error("Consultá los candidatos nuevamente.");
    formulario(
      ctx,
      `Ofrecer plaza a ${nombreSolicitud(ctx, id)}`,
      "<p>El ofrecimiento reserva la plaza; todavía no confirma el ingreso.</p>" +
        motivo(),
      (d) =>
        enviar(ctx, `${p}/ofrecimientos`, {
          operacionId: lista.operacionId,
          solicitudId: id,
          ofertaId: lista.ofertaId,
          propuestaListaId: lista.propuestaListaId,
          motivo: d.motivo,
        }).then(() => ({ destino: "vacantes/ofrecimientos" })),
    );
  } else if (accion === "org-ofrecimiento-historial")
    ver(
      ctx,
      "Historial del ofrecimiento",
      historial(
        "Cambios y respuestas familiares",
        exigirRegistro(datos.ofrecimientos, id).historial,
      ),
    );
  else if (accion === "org-respuesta") {
    const x = exigirRegistro(datos.ofrecimientos, id);
    const opciones = (
      x.estado === "Aceptado"
        ? ["Cancelado"]
        : ["Aceptado", "Rechazado", "Cancelado"]
    ).map((x) => ({ id: x, nombre: x }));
    formulario(
      ctx,
      "Registrar respuesta al ofrecimiento",
      selector("respuesta", "Respuesta", opciones) +
        motivo() +
        campo({
          nombre: "referencia",
          etiqueta: "Constancia familiar (obligatoria al aceptar)",
        }),
      (d) =>
        enviar(ctx, `/ofrecimientos/${id}/respuestas`, {
          ...d,
          revision: x.revision,
        }),
    );
  } else if (accion === "org-confirmar-ofrecimiento") {
    const x = exigirRegistro(datos.ofrecimientos, id);
    formulario(
      ctx,
      "Confirmar ingreso desde espera",
      "<p>Se convertirá la reserva aceptada en un ingreso definitivo.</p>" +
        motivo(),
      (d) =>
        enviar(ctx, `/ofrecimientos/${id}/confirmacion`, {
          revision: x.revision,
          motivo: d.motivo,
        }),
    );
  } else if (["org-baja", "org-baja-historial"].includes(accion)) {
    const x = exigirRegistro(datos.ingresos, id);
    if (accion === "org-baja-historial") {
      const baja = await ctx.api(`/ingresos/${id}/baja`);
      ver(
        ctx,
        "Estado de la baja",
        `<p>${e(baja.estado)}</p><p>${e(baja.motivo ?? "Sin baja registrada")}</p><p>${e(fecha(baja.fechaSolicitud))}</p>`,
      );
    } else
      formulario(
        ctx,
        "Registrar baja definitiva",
        "<p>La plaza se libera cuando también se completa la baja de su asignación. Una baja pendiente puede reintentarse.</p>" +
          campo({
            nombre: "motivo",
            etiqueta: "Motivo de la baja",
            valor: x.motivoBaja ?? "",
            obligatorio: true,
            atributos: x.bajaEnProceso ? "readonly" : "",
          }),
        async (d) => {
          const baja = await enviar(ctx, `/ingresos/${id}/baja`, {
            revisionIngreso: x.revision,
            motivo: d.motivo,
          });
          return {
            mensaje: baja.plazaLiberada
              ? "Baja completada y plaza liberada."
              : "Baja pendiente de coordinación. La plaza permanece ocupada; podés reintentar.",
          };
        },
      );
  }
  return true;
}
