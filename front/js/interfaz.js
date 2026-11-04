export function escapar(valor = "") {
  return String(valor ?? "").replace(
    /[&<>"']/g,
    (caracter) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[
        caracter
      ],
  );
}
export const iniciales = (nombre) =>
  String(nombre || "Usuario")
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((p) => p[0])
    .join("")
    .toUpperCase();
export const fecha = (valor) =>
  valor
    ? new Intl.DateTimeFormat("es-AR", { dateStyle: "medium" }).format(
        new Date(valor.length === 10 ? `${valor}T12:00:00` : valor),
      )
    : "Sin registrar";
export const numero = (valor) =>
  new Intl.NumberFormat("es-AR", { maximumFractionDigits: 12 }).format(valor);
export const textos = {
  RecepcionAbierta: "Recepción abierta",
  RecepcionCerrada: "Recepción cerrada",
  Preparado: "En preparación",
  Ordinaria: "Ordinaria",
  FueraDeTerminoPendienteAutorizacion: "Fuera de término",
  PendienteRevision: "Pendiente de revisión",
  RevisadoManualmente: "Revisado manualmente",
  ManualProvisional: "Manual provisoria",
  Pendiente: "Pendiente",
  Presentada: "Presentada",
  Observada: "Observada",
  Verificada: "Verificada",
  Observado: "Observado",
  DuplicacionConfirmada: "Duplicación confirmada",
  PersonasDiferentes: "Personas diferentes",
  Administrativo: "Administrativo",
  Directivo: "Directivo",
  AdministradorDelSistema: "Administrador del sistema",
};
export const textoEstado = (valor) => textos[valor] ?? valor;
export function etiqueta(estado, variante = "") {
  const aviso = [
    "PendienteRevision",
    "FueraDeTerminoPendienteAutorizacion",
    "Pendiente",
    "ManualProvisional",
    "Preparado",
  ].includes(estado);
  const error = ["Observado", "Observada", "DuplicacionConfirmada"].includes(
    estado,
  );
  return `<span class="etiqueta ${variante ? `etiqueta--${variante}` : aviso ? "etiqueta--aviso" : error ? "etiqueta--error" : ""}"><span class="etiqueta__punto"></span>${escapar(textoEstado(estado))}</span>`;
}
const trazos = {
  inicio:
    '<path d="m3 10 9-7 9 7v10a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1Z"/><path d="M9 21v-8h6v8"/>',
  solicitudes:
    '<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 3h6v4H9zM9 12h6m-6 4h6"/>',
  personas:
    '<circle cx="9" cy="8" r="3"/><path d="M3 21v-2a6 6 0 0 1 12 0v2m1-16a3 3 0 0 1 0 6m2 4a6 6 0 0 1 3 6"/>',
  jardin:
    '<path d="M12 21v-9m0 3C4 16 3 10 4 6c5 0 9 3 8 9Zm0-5c0-6 3-8 8-8 1 5-2 9-8 8Z"/>',
  calendario:
    '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M7 3v4m10-4v4M3 11h18m-13 5h2m4 0h2"/>',
  aulas: '<path d="M3 21h18M5 21V8l7-5 7 5v13m-10 0v-7h6v7M8 9h1m6 0h1"/>',
  ubicacion:
    '<path d="M20 10c0 6-8 12-8 12S4 16 4 10a8 8 0 1 1 16 0Z"/><circle cx="12" cy="10" r="3"/>',
  alerta: '<path d="m12 3 10 18H2Z"/><path d="M12 9v5m0 3h.01"/>',
  ajustes:
    '<path d="M4 7h16M4 17h16"/><circle cx="8" cy="7" r="3" fill="currentColor"/><circle cx="16" cy="17" r="3" fill="currentColor"/>',
  salir: '<path d="M9 4H4v16h5m5-12 4 4-4 4m-6-4h11"/>',
  flecha: '<path d="M4 12h16m-6-6 6 6-6 6"/>',
  mas: '<path d="M12 5v14M5 12h14"/>',
  buscar: '<circle cx="10" cy="10" r="7"/><path d="m15 15 6 6"/>',
  ojo: '<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/>',
  lista: '<path d="M9 6h12M9 12h12M9 18h12M3 6h1m-1 6h1m-1 6h1"/>',
  cerrar: '<path d="m6 6 12 12M6 18 18 6"/>',
  listo: '<path d="m5 12 4 4L19 6"/>',
  reloj: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  candado:
    '<rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 5v2"/>',
  ayuda:
    '<circle cx="12" cy="12" r="9"/><path d="M9.5 8a2.5 2.5 0 1 1 4 2L12 12v2m0 3h.01"/>',
};
export const icono = (nombre) =>
  `<svg class="icono" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${trazos[nombre] ?? trazos.solicitudes}</svg>`;
export const marca = () =>
  '<span class="marca"><img class="marca__imagen" src="./recursos/marca.svg" alt=""><span><span class="marca__nombre">jardín.</span><span class="marca__leyenda">GESTIÓN DE INSCRIPCIONES</span></span></span>';
export function jardinIlustrado() {
  return `<svg viewBox="0 0 360 210" fill="none" aria-hidden="true"><circle cx="284" cy="45" r="23" fill="#dec58f"/><path d="M5 170c80-45 135 30 210 0s104-17 143-12v52H5Z" fill="#8fac83" opacity=".45"/><path d="M9 193c92-40 122 12 208-8s111-5 141 6" stroke="#c7d3a4" stroke-width="1.5"/><path d="M174 181V73m0 65c-42 5-67-19-60-55 38-1 66 20 60 55Zm0-29c-3-40 18-61 54-58 3 36-16 61-54 58Z" fill="#a2bc8e" stroke="#c7d3a4" stroke-width="2"/><path d="M174 139 138 105m37 5 33-38" stroke="#567b55" stroke-width="2"/><path d="M151 164h48l-7 35h-33Z" fill="#dfb785"/><path d="M69 182v-43m0 22c-23 0-31-13-28-29 19 0 30 12 28 29Zm0-17c0-21 10-31 28-30 1 19-10 31-28 30Z" fill="#b7c99b"/><path d="M57 175h26l-4 23H61Z" fill="#d5dfb8"/><path d="M270 188v-56m0 24c-22 1-34-12-32-30 20-1 35 12 32 30Zm0-17c-1-26 12-38 34-36 2 23-12 38-34 36Z" fill="#b4c797"/><path d="M255 179h30l-5 22h-20Z" fill="#e0ca9c"/><path d="M33 76h25m-13-12v25m271-7h13m-6-6v13" stroke="#d2d9b7" opacity=".5" stroke-width="1.5"/></svg>`;
}
export function campo({
  nombre,
  etiqueta: rotulo,
  tipo = "text",
  valor = "",
  obligatorio = false,
  ayuda = "",
  atributos = "",
  completo = false,
}) {
  const id = `campo-${nombre}`;
  return `<label class="campo ${completo ? "campo--completo" : ""}" for="${id}"><span class="campo__etiqueta">${escapar(rotulo)}${obligatorio ? ' <span aria-hidden="true">*</span>' : ""}</span><input id="${id}" class="campo__control" name="${escapar(nombre)}" type="${tipo}" value="${escapar(valor)}" ${obligatorio ? "required" : ""} ${atributos} ${ayuda ? `aria-describedby="${id}-ayuda"` : ""}>${ayuda ? `<span id="${id}-ayuda" class="campo__ayuda">${escapar(ayuda)}</span>` : ""}</label>`;
}
export function seleccion({
  nombre,
  etiqueta: rotulo,
  opciones,
  valor = "",
  obligatorio = false,
}) {
  return `<label class="campo"><span class="campo__etiqueta">${escapar(rotulo)}${obligatorio ? " *" : ""}</span><select name="${escapar(nombre)}" class="campo__control" ${obligatorio ? "required" : ""}>${opciones.map((o) => `<option value="${escapar(o.id)}" ${String(o.id) === String(valor) ? "selected" : ""}>${escapar(o.nombre)}</option>`).join("")}</select></label>`;
}
export const aviso = (mensaje, variante = "") =>
  `<div class="aviso ${variante ? `aviso--${variante}` : ""}" role="status">${icono("ayuda")}<div class="aviso__contenido">${mensaje}</div></div>`;
export function vacio({
  titulo,
  descripcion,
  accion = "",
  dibujo = "solicitudes",
}) {
  return `<div class="vacio"><div class="vacio__icono">${icono(dibujo)}</div><h2>${escapar(titulo)}</h2><p>${escapar(descripcion)}</p>${accion}</div>`;
}
export const errorFormulario = () =>
  '<p class="formulario__error" data-error role="alert" hidden></p>';
let temporizador;
export function notificar(mensaje, esError = false) {
  clearTimeout(temporizador);
  const contenedor = document.getElementById("notificaciones");
  contenedor.replaceChildren();
  const nota = document.createElement("div");
  nota.className = `notificacion${esError ? " notificacion--error" : ""}`;
  nota.textContent = mensaje;
  contenedor.append(nota);
  temporizador = setTimeout(() => contenedor.replaceChildren(), 6500);
}
export const direccionTexto = (d) =>
  d
    ? `${d.calle} ${d.numero}, ${d.localidad}, ${d.provincia}`
    : "Sin dirección registrada";
