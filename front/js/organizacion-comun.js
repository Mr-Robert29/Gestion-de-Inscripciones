import { escapar as e, etiqueta, fecha } from "./interfaz.js";
import { tarjeta } from "./compartidos.js";

export const rutaPeriodo = (ctx) =>
  `/periodos/${encodeURIComponent(ctx.estado.periodo.id)}`;
export const rutaInstitucional = (ctx) =>
  `/instituciones/${encodeURIComponent(ctx.estado.institucion.id)}/periodos/${encodeURIComponent(ctx.estado.periodo.id)}`;
export const nombreTurno = (ctx, id) =>
  ctx.estado.turnos.find((x) => x.id === id)?.nombre ?? "Sin turno";
export const nombreAula = (ctx, id) =>
  ctx.estado.aulas.find((x) => x.id === id)?.nombre ?? "Aula no disponible";
export const nombreSolicitud = (ctx, id) =>
  ctx.nombresSolicitudes?.get(id) ?? `Solicitud ${String(id).slice(0, 8)}`;
export const enlaceSolicitud = (ctx, id) =>
  `<a href="#solicitud/${encodeURIComponent(id)}">${e(nombreSolicitud(ctx, id))}</a>`;
export const boton = (accion, texto, id = "", deshabilitado = false) =>
  `<button class="boton boton--secundario boton--pequeno" data-accion="${e(accion)}" data-id="${e(id)}" ${deshabilitado ? "disabled" : ""}>${e(texto)}</button>`;
export function tabla(columnas, filas) {
  return filas.length
    ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr>${columnas.map((x) => `<th scope="col">${e(x)}</th>`).join("")}</tr></thead><tbody>${filas.map((f) => `<tr>${f.map((c) => `<td>${c ?? "—"}</td>`).join("")}</tr>`).join("")}</tbody></table></div>`
    : '<p class="tarjeta__cuerpo">No hay registros para esta consulta.</p>';
}
export function pestanas(tipo, activa, opciones) {
  return `<nav class="pestanas" aria-label="Etapas de ${tipo}">${opciones.map(([ruta, texto]) => `<a class="pestanas__enlace ${activa === ruta ? "pestanas__enlace--activo" : ""}" href="#${ruta}" ${activa === ruta ? 'aria-current="page"' : ""}>${e(texto)}</a>`).join("")}</nav>`;
}
export const resumen = (titulo, filas, accion = "") =>
  tarjeta({ titulo, accion, cuerpo: filas });
export function historial(titulo, cambios) {
  return resumen(
    titulo,
    tabla(
      ["Fecha", "Usuario", "Cambio", "Motivo / referencia"],
      cambios.map((x) => [
        e(fecha(x.fecha ?? x.fechaCreacion)),
        e(x.usuarioId ?? x.usuarioResolucion ?? "—"),
        e(
          x.estadoNuevo ??
            x.estado ??
            `${x.edadesAnteriores ?? ""} → ${x.edadesNuevas ?? ""}`,
        ),
        e(
          [x.motivo ?? x.motivoResolucion, x.referencia]
            .filter(Boolean)
            .join(" · "),
        ),
      ]),
    ),
  );
}
export async function cargarNombres(ctx) {
  if (ctx.estado.previa) return;
  const clave = `${ctx.estado.institucion.id}/${ctx.estado.periodo.id}`;
  const nombres = new Map();
  for (let pagina = 1; ; pagina++) {
    const datos = await ctx.api(
      `/instituciones/${ctx.estado.institucion.id}/solicitudes/buscar`,
      {
        metodo: "POST",
        datos: { periodoId: ctx.estado.periodo.id, pagina, tamanoPagina: 100 },
      },
    );
    for (const x of datos.resultados)
      nombres.set(x.id, `${x.nombre} ${x.apellido}`);
    if (!datos.hayMas) break;
  }
  if (`${ctx.estado.institucion?.id}/${ctx.estado.periodo?.id}` === clave)
    ctx.nombresSolicitudes = nombres;
}
export function vistaPreviaOrganizacion() {
  return resumen(
    "Recorrido de organización",
    "<p>Iniciá sesión para consultar las prioridades, generar propuestas, registrar ofrecimientos y organizar las secciones con datos del servidor. La vista previa no envía operaciones ni representa resultados reales.</p>",
  );
}
