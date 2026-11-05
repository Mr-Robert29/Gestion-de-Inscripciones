import { escapar as e, etiqueta, fecha } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import {
  rutaPeriodo,
  nombreTurno,
  nombreAula,
  boton,
  tabla,
  pestanas,
  resumen,
  vistaPreviaOrganizacion,
} from "./organizacion-comun.js";

export async function secciones(ruta, ctx) {
  const distribucionId = ruta.split("/")[2];
  const cabecera =
    encabezado({
      titulo: "Secciones y distribución",
      descripcion:
        "Organizá a los aspirantes admitidos sin superar la capacidad ni excluir edades asignadas.",
    }) +
    pestanas(
      "secciones",
      ruta.split("/")[1] === "distribuciones"
        ? "secciones/distribuciones"
        : "secciones",
      [
        ["secciones", "Secciones"],
        ["secciones/distribuciones", "Distribuciones"],
      ],
    );
  if (ctx.estado.previa) return cabecera + vistaPreviaOrganizacion();
  const secciones = await ctx.api(`${rutaPeriodo(ctx)}/secciones`);
  ctx.organizacion = { secciones };
  if (ruta.split("/")[1] === "distribuciones") {
    if (!distribucionId) {
      const propuestas = await ctx.api(
        `${rutaPeriodo(ctx)}/propuestas-distribucion`,
      );
      return (
        cabecera +
        resumen(
          "Propuestas de distribución",
          tabla(
            ["Versión", "Estado", "Fecha", "Acción"],
            propuestas.map((x) => [
              e(x.version),
              etiqueta(x.estado),
              e(fecha(x.fechaCreacion)),
              `<a class="boton boton--secundario boton--pequeno" href="#secciones/distribuciones/${encodeURIComponent(x.id)}">Revisar distribución</a>`,
            ]),
          ),
          boton("org-generar-distribucion", "Generar distribución"),
        )
      );
    }
    const distribucion = await ctx.api(
      `/propuestas-distribucion/${encodeURIComponent(distribucionId)}`,
    );
    if (distribucion.periodoId !== ctx.estado.periodo.id)
      throw new Error("La distribución pertenece a otro período.");
    ctx.organizacion.distribucion = distribucion;
    const editable = distribucion.estado === "Borrador" && ctx.puedeResolver();
    return (
      cabecera +
      resumen(
        `Distribución ${distribucion.version}`,
        `<p>${etiqueta(distribucion.estado)}</p>` +
          tabla(
            ["Ingreso", "Edad", "Turno", "Sección", "Motivo", "Acción"],
            distribucion.asignaciones.map((x) => [
              e(`Ingreso ${x.ingresoId.slice(0, 8)}`),
              e(x.edad),
              e(nombreTurno(ctx, x.turnoId)),
              e(
                secciones.find((s) => s.id === x.seccionId)?.nombre ??
                  "Sin sección",
              ),
              e(x.motivoModificacion ?? x.motivoSinSeccion ?? "—"),
              editable
                ? boton("org-asignar", "Revisar asignación", x.ingresoId)
                : "",
            ]),
          ),
        editable
          ? boton(
              "org-confirmar-distribucion",
              "Confirmar distribución",
              distribucion.id,
            )
          : "",
      )
    );
  }
  return (
    cabecera +
    resumen(
      "Secciones del período",
      tabla(
        ["Nombre", "Aula / turno", "Tipo", "Edades", "Capacidad", "Acciones"],
        secciones.map((x) => [
          e(x.nombre),
          e(`${nombreAula(ctx, x.aulaId)} · ${nombreTurno(ctx, x.turnoId)}`),
          etiqueta(x.tipo),
          e(x.edadesAdmitidas.join(", ")),
          e(x.capacidad),
          boton("org-composicion-historial", "Historial", x.id) +
            (ctx.puedeResolver() && x.tipo === "Pura"
              ? boton("org-composicion", "Ampliar a multiedad", x.id)
              : ""),
        ]),
      ),
      ctx.puedeResolver()
        ? boton("org-definir-seccion", "Definir sección")
        : "",
    )
  );
}
