import { escapar as e, etiqueta, fecha } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import {
  rutaPeriodo,
  nombreTurno,
  nombreAula,
  enlaceSolicitud,
  boton,
  tabla,
  pestanas,
  resumen,
  cargarNombres,
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
  const ofertasHabilitadas = (ctx.estado.ofertasPublicadas ?? []).filter((oferta) => oferta.capacidad > 0);
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
    await cargarNombres(ctx);
    const distribucion = await ctx.api(
      `/propuestas-distribucion/${encodeURIComponent(distribucionId)}`,
    );
    if (distribucion.periodoId !== ctx.estado.periodo.id)
      throw new Error("La distribución pertenece a otro período.");
    ctx.organizacion.distribucion = distribucion;
    const editable = distribucion.estado === "Borrador" && ctx.puedeResolver();
    const asignadas = distribucion.asignaciones.filter((x) => x.seccionId !== null);
    const sinSeccion = distribucion.asignaciones.filter((x) => x.seccionId === null);
    const metricas = `<div class="distribucion__metricas"><article><span>Ingresos incluidos en esta propuesta</span><strong>${distribucion.asignaciones.length}</strong></article><article><span>Con sección asignada</span><strong>${asignadas.length}</strong></article><article><span>Sin sección asignada</span><strong>${sinSeccion.length}</strong></article></div>`;
    const ocupacionSecciones = tabla(
      ["Sección", "Capacidad total", "Asignaciones sugeridas"],
      secciones.map((seccion) => {
        const cantidad = asignadas.filter((x) => x.seccionId === seccion.id).length;
        return [
          e(seccion.nombre),
          e(seccion.capacidad),
          e(cantidad),
        ];
      }),
    );
    return (
      cabecera +
      resumen(
        `Distribución ${distribucion.version}`,
        `<p>${etiqueta(distribucion.estado)} · Generada el ${e(fecha(distribucion.fechaCreacion))}</p><p>Esta versión organiza ingresos confirmados y activos del período que todavía no tienen una sección asignada. Los borradores de propuesta no se incluyen.</p>${metricas}<h3>Asignaciones sugeridas por sección</h3>${ocupacionSecciones}<p class="formulario__nota">La capacidad disponible ya considera las asignaciones confirmadas previamente. Esta tabla cuenta las nuevas asignaciones sugeridas en esta versión.</p><h3>Detalle de aspirantes</h3>` +
          tabla(
            ["Aspirante", "Edad al ingreso", "Turno", "Sección asignada", "Motivo / observación", "Acción"],
            distribucion.asignaciones.map((x) => [
              x.solicitudId
                ? enlaceSolicitud(ctx, x.solicitudId)
                : e(`Ingreso ${x.ingresoId.slice(0, 8)}`),
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
        ? ofertasHabilitadas.length
          ? boton("org-definir-seccion", "Definir sección")
          : '<p class="tarjeta__descripcion">No hay ofertas habilitadas publicadas para este período. Configurá y publicá una oferta antes de definir secciones.</p>'
        : "",
    )
  );
}
