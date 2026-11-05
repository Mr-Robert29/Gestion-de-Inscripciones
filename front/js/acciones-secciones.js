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

export async function atenderSecciones(elemento, ctx) {
  const accion = elemento.dataset.accion;
  const id = elemento.dataset.id;
  const p = ctx.estado.periodo ? rutaPeriodo(ctx) : null;
  const datos = ctx.organizacion ?? {};
  if (accion === "org-definir-seccion")
    formulario(
      ctx,
      "Definir sección",
      campo({ nombre: "nombre", etiqueta: "Nombre", obligatorio: true }) +
        selector("aulaId", "Aula", ctx.estado.aulas) +
        selector("turnoId", "Turno", ctx.estado.turnos) +
        selector(
          "edades",
          "Composición",
          ["3", "4", "5", "3,4", "4,5"].map((x) => ({
            id: x,
            nombre: x.includes(",") ? `Multiedad ${x}` : `Pura de ${x}`,
          })),
        ),
      (d) =>
        enviar(ctx, `${p}/secciones`, {
          periodoId: ctx.estado.periodo.id,
          nombre: d.nombre,
          aulaId: d.aulaId,
          turnoId: d.turnoId,
          edadesAdmitidas: d.edades.split(",").map(Number),
        }),
    );
  else if (["org-composicion", "org-composicion-historial"].includes(accion)) {
    const x = exigirRegistro(datos.secciones, id);
    const ruta = `${p}/secciones/${id}/composicion`;
    if (accion === "org-composicion-historial")
      ver(
        ctx,
        "Historial de composición",
        historial(x.nombre, await ctx.api(`${ruta}/historial`)),
      );
    else
      formulario(
        ctx,
        "Ampliar sección a multiedad",
        `<p>${e(x.nombre)} · ${e(x.edadesAdmitidas.join(", "))} años. Se comprobarán capacidad y compatibilidad.</p>` +
          selector(
            "edades",
            "Nueva composición",
            ["3,4", "4,5"]
              .filter((c) =>
                c.split(",").map(Number).includes(x.edadesAdmitidas[0]),
              )
              .map((c) => ({ id: c, nombre: c })),
          ) +
          motivo(),
        (d) =>
          enviar(ctx, ruta, {
            nuevasEdades: d.edades.split(",").map(Number),
            motivo: d.motivo,
            revision: x.revision,
          }),
      );
  } else if (accion === "org-generar-distribucion")
    formulario(
      ctx,
      "Generar distribución",
      "<p>Se propondrá una sección compatible para cada ingreso activo. Generar no confirma las asignaciones.</p>",
      async () => {
        const x = await enviar(ctx, `${p}/propuestas-distribucion`, {});
        return { destino: `secciones/distribuciones/${x.id}` };
      },
    );
  else if (accion === "org-asignar") {
    const x = datos.distribucion.asignaciones.find((x) => x.ingresoId === id);
    const opciones = datos.secciones
      .filter(
        (s) => s.turnoId === x.turnoId && s.edadesAdmitidas.includes(x.edad),
      )
      .map((s) => ({ id: s.id, nombre: s.nombre }));
    formulario(
      ctx,
      "Revisar asignación",
      selector(
        "seccionId",
        "Sección",
        [{ id: "", nombre: "Sin sección" }, ...opciones],
        x.seccionId ?? "",
        false,
      ) + motivo(),
      (d) =>
        enviar(
          ctx,
          `/propuestas-distribucion/${datos.distribucion.id}/asignaciones`,
          {
            ingresoId: id,
            seccionId: d.seccionId || null,
            motivoSinSeccion: d.seccionId ? null : d.motivo,
            motivo: d.motivo,
          },
          "PATCH",
        ),
    );
  } else if (accion === "org-confirmar-distribucion")
    formulario(
      ctx,
      "Confirmar distribución",
      '<p>Se guardarán las asignaciones definitivas. Las propuestas desactualizadas no pueden confirmarse.</p><label class="casilla"><input type="checkbox" required>Revisé las asignaciones y los casos sin sección.</label>',
      () =>
        enviar(
          ctx,
          `/propuestas-distribucion/${datos.distribucion.id}/confirmaciones`,
          {},
        ),
    );
  return true;
}
