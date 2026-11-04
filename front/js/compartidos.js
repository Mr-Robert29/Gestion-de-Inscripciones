import { escapar as e, icono, etiqueta, vacio } from "./interfaz.js";
import { solicitudesEjemplo } from "./datos-previa.js";
export const encabezado = ({ titulo, descripcion, accion = "" }) =>
  `<div class="contenido__cabecera"><div><h1>${e(titulo)}</h1><p class="contenido__descripcion">${e(descripcion)}</p></div>${accion}</div>`;
export const botonNuevo = ({ texto, accion }) =>
  `<button class="boton" data-accion="${accion}">${icono("mas")}${texto}</button>`;
export const base = (estado) =>
  `/instituciones/${encodeURIComponent(estado.institucion.id)}`;
export const tarjeta = ({ titulo, descripcion = "", cuerpo, accion = "" }) =>
  `<section class="tarjeta"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">${titulo}</h2>${descripcion ? `<p class="tarjeta__descripcion">${descripcion}</p>` : ""}</div>${accion}</div><div class="tarjeta__cuerpo">${cuerpo}</div></section>`;
export const volver =
  '<a class="boton boton--secundario" href="#solicitudes">Volver a solicitudes</a>';
export async function buscarSolicitudes(ctx) {
  if (ctx.estado.previa) {
    const f = ctx.estado.filtros;
    const resultados = solicitudesEjemplo.filter(
      (s) =>
        (!f.nombre ||
          s.nombre.toLowerCase().includes(f.nombre.toLowerCase())) &&
        (!f.apellido ||
          s.apellido.toLowerCase().includes(f.apellido.toLowerCase())) &&
        (!f.dni || s.dni === f.dni),
    );
    return { resultados, hayMas: false, pagina: 1 };
  }
  return ctx.api(`${base(ctx.estado)}/solicitudes/buscar`, {
    metodo: "POST",
    datos: {
      ...ctx.estado.filtros,
      periodoId: ctx.estado.periodo.id,
      pagina: ctx.estado.pagina,
      tamanoPagina: 20,
    },
  });
}
export function tablaSolicitudes({
  resultados,
  modo = "solicitud",
  resumen = false,
}) {
  if (!resultados.length)
    return vacio({
      titulo: "Todavía no hay solicitudes",
      descripcion:
        "Las solicitudes de este período aparecerán acá. También podés ajustar los filtros de búsqueda.",
      dibujo: "solicitudes",
    });
  return `<div class="tabla__contenedor"><table class="tabla ${resumen ? "tabla--resumen" : ""}"><caption class="solo-lector">Solicitudes del período seleccionado</caption><thead><tr><th>Aspirante</th><th>Documento</th><th>Grupo etario</th><th>Recepción</th><th><span class="solo-lector">Acciones</span></th></tr></thead><tbody>${resultados.map((s, i) => `<tr><td><div class="tabla__persona"><span class="avatar avatar--${["verde", "lila", "azul"][i % 3]}">${e(`${s.nombre[0] ?? ""}${s.apellido[0] ?? ""}`)}</span><div><div class="tabla__nombre">${e(s.nombre)} ${e(s.apellido)}</div><div class="tabla__detalle">Solicitud <span class="tabla__referencia">${e(s.id.slice(0, 8))}</span></div></div></div></td><td>${s.dni ? e(s.dni) : '<span class="tabla__detalle">Sin DNI declarado</span>'}</td><td>${s.sala ? `Sala de ${e(s.sala)} años` : etiqueta("PendienteRevision")}</td><td>${etiqueta(s.estadoRecepcion)}</td><td class="tabla__accion"><a class="boton boton--texto boton--pequeno" href="#solicitud/${encodeURIComponent(s.id)}/${modo}">${modo === "domicilio" ? "Revisar domicilio" : modo === "documentos" ? "Ver antecedentes" : "Ver ficha"} ${icono("flecha")}</a></td></tr>`).join("")}</tbody></table></div>`;
}
