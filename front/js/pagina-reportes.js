import { escapar as e, etiqueta, fecha } from "./interfaz.js";
import { encabezado } from "./compartidos.js";
import {
  rutaInstitucional,
  tabla,
  boton,
  resumen,
  enlaceSolicitud,
  cargarNombres,
  historial,
  vistaPreviaOrganizacion,
} from "./organizacion-comun.js";
export async function reportes(ctx) {
  const cabecera = encabezado({
    titulo: "Resultados e historial",
    descripcion:
      "Consultá los antecedentes del período seleccionado y exportá la información autorizada.",
  });
  if (ctx.estado.previa) return cabecera + vistaPreviaOrganizacion();
  await cargarNombres(ctx);
  const datos = await ctx.api(`${rutaInstitucional(ctx)}/reportes`);
  ctx.organizacion = { reporte: datos };
  return (
    cabecera +
    resumen(
      "Resumen del período",
      `<p>Institución: ${e(ctx.estado.institucion.nombre)} · Ciclo ${e(ctx.estado.periodo.ciclo)} · Consulta: ${e(fecha(datos.fechaConsulta))}</p><div class="indicadores">${[
        ["Solicitudes", datos.solicitudes.length],
        [
          "Ingresos activos",
          datos.ingresos.filter((x) => x.activo && !x.bajaEnProceso).length,
        ],
        ["En espera", datos.listaEspera.totalEnEspera],
        ["Pendientes", datos.listaEspera.totalPendientes],
        ["Secciones", datos.secciones.length],
      ]
        .map(([n, v]) => `<p><strong>${v}</strong> ${e(n)}</p>`)
        .join(
          "",
        )}</div><div class="formulario__acciones">${boton("org-exportar-reportes", "Descargar reporte CSV")}${boton("org-nomina", "Ver nómina sin vacante")}${boton("org-exportar-nomina", "Descargar nómina CSV")}</div>`,
    ) +
    resumen(
      "Ingresos y bajas",
      tabla(
        ["Aspirante", "Estado", "Fecha", "Motivo"],
        datos.ingresos.map((x) => [
          enlaceSolicitud(ctx, x.solicitudId),
          etiqueta(
            x.bajaEnProceso ? "Baja pendiente" : x.activo ? "Activo" : "Baja",
          ),
          e(fecha(x.fechaBaja ?? x.fecha)),
          e(x.motivoBaja ?? "—"),
        ]),
      ),
    ) +
    historial("Cambios de composición", datos.cambiosComposicion) +
    resumen(
      "Historial de inscripciones",
      tabla(
        ["Acción", "Fecha", "Datos del cambio"],
        datos.historialInscripciones.map((x) => [
          e(x.accion),
          e(fecha(x.fecha)),
          e(x.datos),
        ]),
      ),
    ) +
    resumen(
      "Distribuciones conservadas",
      tabla(
        ["Versión", "Estado", "Fecha", "Consultar"],
        datos.propuestasDistribucion.map((x) => [
          e(x.version),
          etiqueta(x.estado),
          e(fecha(x.fechaConfirmacion ?? x.fechaCreacion)),
          `<a href="#secciones/distribuciones/${encodeURIComponent(x.id)}">Ver distribución</a>`,
        ]),
      ),
    )
  );
}
