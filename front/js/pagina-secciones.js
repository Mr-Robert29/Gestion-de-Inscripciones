import { escapar as e, etiqueta, fecha } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import {
  rutaPeriodo,
  nombreTurno,
  nombreAula,
  enlaceSolicitud,
  nombreSolicitud,
  boton,
  tabla,
  pestanas,
  resumen,
  cargarNombres,
  vistaPreviaOrganizacion,
} from "./organizacion-comun.js?v=20261117-distribucion-filtros-v1";

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
    const ordenSeccion = new Map(secciones.map((seccion, indice) => [seccion.id, indice]));
    const nombreAsignacion = (x) => x.solicitudId
      ? nombreSolicitud(ctx, x.solicitudId)
      : `Ingreso ${x.ingresoId.slice(0, 8)}`;
    const asignacionesOrdenadas = [...distribucion.asignaciones].sort((a, b) => {
      const ordenA = a.seccionId === null ? secciones.length + 1 : ordenSeccion.get(a.seccionId) ?? secciones.length;
      const ordenB = b.seccionId === null ? secciones.length + 1 : ordenSeccion.get(b.seccionId) ?? secciones.length;
      return ordenA - ordenB || nombreAsignacion(a).localeCompare(nombreAsignacion(b), "es", { sensitivity: "base" });
    });
    const turnosDistribucion = [...new Map([
      ...(ctx.estado.turnos ?? []),
      ...distribucion.asignaciones.map((x) => ({ id: x.turnoId, nombre: nombreTurno(ctx, x.turnoId) })),
    ].map((turno) => [turno.id, turno])).values()];
    const opcionesSeccion = secciones.map((seccion) => `<option value="${e(seccion.id)}">${e(seccion.nombre)}</option>`).join("");
    const opcionesTurno = turnosDistribucion.map((turno) => `<option value="${e(turno.id)}">${e(turno.nombre)}</option>`).join("");
    const filasDetalle = asignacionesOrdenadas.map((x) => {
      const nombre = nombreAsignacion(x);
      const dni = ctx.solicitudesPorId?.get(x.solicitudId)?.dni ?? "";
      const dniNormalizado = String(dni).replace(/\D/g, "");
      const seccion = secciones.find((s) => s.id === x.seccionId)?.nombre ?? "Sin sección";
      return `<tr data-distribucion-fila data-nombre="${e(nombre)}" data-dni="${e(dniNormalizado)}" data-seccion="${e(x.seccionId ?? "sin-seccion")}" data-turno="${e(x.turnoId)}"><td>${x.solicitudId ? enlaceSolicitud(ctx, x.solicitudId) : e(nombre)}</td><td>${e(x.edad)}</td><td>${e(nombreTurno(ctx, x.turnoId))}</td><td>${e(seccion)}</td><td>${e(x.motivoModificacion ?? x.motivoSinSeccion ?? "—")}</td><td>${editable ? boton("org-asignar", "Revisar asignación", x.ingresoId) : ""}</td></tr>`;
    }).join("");
    const filtroDetalle = `<div class="distribucion__filtros" data-distribucion-filtros><label class="campo"><span class="campo__etiqueta">Buscar por nombre o DNI</span><input class="campo__control" type="search" data-buscar-distribucion placeholder="Escribí un nombre o DNI" autocomplete="off"></label><label class="campo"><span class="campo__etiqueta">Sección asignada</span><select class="campo__control" data-filtro-distribucion-seccion><option value="todas">Todas las secciones</option>${opcionesSeccion}<option value="sin-seccion">Sin sección</option></select></label><label class="campo"><span class="campo__etiqueta">Turno</span><select class="campo__control" data-filtro-distribucion-turno><option value="todos">Todos los turnos</option>${opcionesTurno}</select></label><span class="distribucion__filtro-resumen" data-resumen-distribucion>${distribucion.asignaciones.length} aspirantes</span></div>`;
    const tablaDetalle = filasDetalle
      ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr>${["Aspirante", "Edad al ingreso", "Turno", "Sección asignada", "Motivo / observación", "Acción"].map((x) => `<th scope="col">${e(x)}</th>`).join("")}</tr></thead><tbody>${filasDetalle}</tbody></table></div>`
      : tabla(["Aspirante", "Edad al ingreso", "Turno", "Sección asignada", "Motivo / observación", "Acción"], []);
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
        `<p>${etiqueta(distribucion.estado)} · Generada el ${e(fecha(distribucion.fechaCreacion))}</p><p>Esta versión organiza ingresos confirmados y activos del período que todavía no tienen una sección asignada. Los borradores de propuesta no se incluyen.</p>${metricas}<h3>Asignaciones sugeridas por sección</h3>${ocupacionSecciones}<p class="formulario__nota">La capacidad disponible ya considera las asignaciones confirmadas previamente. Esta tabla cuenta las nuevas asignaciones sugeridas en esta versión.</p><h3>Detalle de aspirantes</h3>${filtroDetalle}${tablaDetalle}`,
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
