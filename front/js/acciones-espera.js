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

export async function atenderEspera(elemento, ctx) {
  const accion = elemento.dataset.accion;
  const id = elemento.dataset.id;
  const p = ctx.estado.periodo ? rutaPeriodo(ctx) : null;
  const datos = ctx.organizacion ?? {};
  if (accion === "org-limpiar-filtros-espera") {
    ctx.estado.filtrosEspera = { busqueda: "", sala: "", turnoPreferidoId: "" };
    ctx.estado.pagina = 1;
    await ctx.actualizar();
  } else if (accion === "org-candidatos") {
    // La página de destino realiza la consulta. Evita disparar el mismo GET aquí y al navegar.
    ctx.navegar("vacantes/candidatos");
  } else if (accion === "org-ofrecer") {
    const lista = ctx.candidatosOferta;
    if (!lista || lista.periodoId !== ctx.estado.periodo.id)
      throw new Error("Consultá los candidatos nuevamente.");
    const candidato = lista.candidatos.find((x) => x.candidato.solicitudId === id);
    if (!candidato || !lista.propuestaListaId)
      throw new Error("El candidato ya no está disponible. Volvé a consultar la lista.");
    const ofertasCompatibles = candidato.ofertasCompatibles.map((oferta) => ({
      id: oferta.ofertaId,
      nombre: `${nombreAula(ctx, oferta.aulaId)} · ${nombreTurno(ctx, oferta.turnoId)} · ${oferta.nombreSeccion ?? "Sin sección"} (${oferta.edadesAdmitidas?.length ? `admite ${oferta.edadesAdmitidas.join(" y ")} años` : "sin composición"})`,
    }));
    const operacionId = crypto.randomUUID();
    formulario(
      ctx,
      `Ofrecer plaza a ${nombreSolicitud(ctx, id)}`,
      "<p>El ofrecimiento reserva la plaza; todavía no confirma el ingreso.</p>" +
        selector("ofertaId", "Aula, turno y sección compatibles", ofertasCompatibles) +
        motivo(motivos.ofrecerVacante),
      (d) =>
        enviar(ctx, `${p}/ofrecimientos`, {
          operacionId,
          solicitudId: id,
          ofertaId: d.ofertaId,
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
        motivo(motivos.respuestaOferta) +
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
        motivo(motivos.confirmarIngreso),
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
          motivo(motivos.bajaIngreso, {
            etiqueta: "Motivo de la baja",
            valor: x.motivoBaja ?? "",
            soloLectura: x.bajaEnProceso,
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
