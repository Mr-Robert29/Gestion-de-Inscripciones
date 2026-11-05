import { solicitar, configurarDireccion, establecerToken } from "./api.js";
import { configuracion } from "./configuracion.js";
import {
  escapar,
  iniciales,
  icono,
  marca,
  jardinIlustrado,
  campo,
  notificar,
  vacio,
} from "./interfaz.js";
import {
  institucionEjemplo,
  periodosEjemplo,
  aulasEjemplo,
  turnosEjemplo,
  ofertasEjemplo,
} from "./datos-previa.js";
import { presentarPagina, titulos } from "./paginas.js";
import { atenderAccion, atenderFormulario } from "./acciones.js";
import { actualizarIndicadoresClave } from "./validacion-clave.js";

const raiz = document.getElementById("aplicacion");
const estado = {
  usuario: null,
  previa: false,
  instituciones: [],
  institucion: null,
  periodos: [],
  periodo: null,
  aulas: [],
  turnos: [],
  ofertas: [],
  turnosSolicitud: [],
  pagina: 1,
  filtros: {},
  borrador: null,
  borradores: [],
};
let versionVista = 0;
let vencimiento;
const contexto = {
  estado,
  api: solicitar,
  navegar,
  actualizar: () => cargarVista(),
  notificar,
  abrirDialogo,
  cerrarDialogo,
  puedeEditar,
  puedeCrearInstitucion: esRoot,
  puedeResolver,
};

function esRoot() {
  return estado.usuario?.roles?.includes("AdministradorDelSistema") ?? false;
}
function rolActual() {
  return esRoot()
    ? "Administrador del sistema"
    : (estado.usuario?.accesos?.find(
        (a) => a.institucionId === estado.institucion?.id,
      )?.rol ??
        estado.usuario?.roles?.[0] ??
        "Personal");
}
function puedeEditar() {
  return estado.previa || esRoot() || rolActual() === "Directivo";
}
function puedeResolver() {
  return puedeEditar();
}
function navegar(destino) {
  if (location.hash === `#${destino}`) cargarVista();
  else location.hash = destino;
}
function cerrarDialogo() {
  document.getElementById("dialogo").close();
}
function abrirDialogo({ titulo, descripcion = "", contenido }) {
  const dialogo = document.getElementById("dialogo");
  dialogo.innerHTML = `<div class="dialogo__cabecera"><div><h2 id="titulo-dialogo">${escapar(titulo)}</h2><p class="tarjeta__descripcion">${escapar(descripcion)}</p></div><button class="boton boton--texto boton--icono" data-accion="cerrar-dialogo" aria-label="Cerrar ventana">${icono("cerrar")}</button></div>${contenido}`;
  dialogo.showModal();
}

function salir(mensaje = "") {
  versionVista++;
  clearTimeout(vencimiento);
  establecerToken(null);
  Object.assign(estado, {
    usuario: null,
    previa: false,
    instituciones: [],
    institucion: null,
    periodos: [],
    periodo: null,
    aulas: [],
    turnos: [],
    ofertas: [],
    borrador: null,
    borradores: [],
    filtros: {},
    pagina: 1,
  });
  cerrarDialogo();
  location.hash = "acceso";
  presentarAcceso();
  if (mensaje) notificar(mensaje);
}
window.addEventListener("sesion-vencida", () =>
  salir("Tu sesión venció. Volvé a ingresar para continuar."),
);

function presentarAcceso() {
  document.title = "Ingresar · Jardín";
  raiz.innerHTML = `<main class="acceso" id="contenido"><section class="acceso__presentacion"><div>${marca()}</div><div class="acceso__mensaje"><p class="acceso__epigrafe">Cada comienzo cuenta</p><h2 class="acceso__titulo">Un lugar para<br>crecer.<br><em>Una gestión<br>más simple.</em></h2><p class="acceso__descripcion">Acompañá cada inscripción, organizá tu jardín y dedicá más tiempo a lo que importa.</p></div><div class="acceso__ilustracion">${jardinIlustrado()}</div><p class="acceso__pie">Un espacio de trabajo para tu comunidad educativa.</p></section><section class="acceso__formulario" aria-label="Inicio de sesión"><div class="acceso__panel"><p class="acceso__saludo">Bienvenido a tu jardín</p><h1>Iniciá sesión</h1><p class="acceso__instruccion">Ingresá con tu cuenta para continuar.</p><form class="formulario" id="formulario-acceso">${campo({ nombre: "correo", etiqueta: "Correo electrónico", tipo: "email", obligatorio: true, atributos: 'autocomplete="username" placeholder="nombre@institucion.edu.ar"' })}<div class="campo"><label class="campo__etiqueta" for="clave-acceso">Contraseña *</label><div class="acceso__clave"><input id="clave-acceso" class="campo__control" type="password" name="clave" required autocomplete="current-password" placeholder="Ingresá tu contraseña"><button type="button" class="boton boton--texto boton--icono acceso__mostrar" data-accion="mostrar-clave" aria-label="Mostrar contraseña" aria-pressed="false">${icono("ojo")}</button></div></div><p class="formulario__error" data-error role="alert" hidden></p><button class="boton boton--ancho" type="submit">Ingresar al sistema ${icono("flecha")}</button></form><p class="acceso__seguridad">${icono("candado")} Acceso exclusivo para personal autorizado</p><div class="acceso__previa"><button class="boton boton--secundario boton--ancho" data-accion="vista-previa">Explorar la vista previa ${icono("ojo")}</button><p>Recorré las pantallas con datos ficticios. Sin iniciar sesión.</p></div><details class="acceso__detalle"><summary>Configurar conexión al servidor</summary>${campo({ nombre: "servidor", etiqueta: "Dirección de la API", tipo: "url", valor: configuracion.direccionApi, ayuda: "Para desarrollo local. La contraseña se envía únicamente al servidor indicado." })}</details></div></section></main>`;
}

async function entrar(formulario) {
  const boton = formulario.querySelector('button[type="submit"]');
  const error = formulario.querySelector("[data-error]");
  const datos = new FormData(formulario);
  const intento = ++versionVista;
  boton.disabled = true;
  boton.textContent = "Ingresando…";
  error.hidden = true;
  try {
    configurarDireccion(document.getElementById("campo-servidor").value);
    const acceso = await solicitar("/Autenticacion/iniciar-sesion", {
      metodo: "POST",
      datos: {
        correoElectronico: datos.get("correo"),
        clave: datos.get("clave"),
      },
    });
    if (intento !== versionVista) return;
    if (
      !acceso?.token ||
      !Number.isFinite(Date.parse(acceso.expiracion)) ||
      Date.parse(acceso.expiracion) <= Date.now()
    )
      throw new Error("El servidor no devolvió una sesión válida.");
    establecerToken(acceso.token);
    const usuario = await solicitar("/Autenticacion/mi-acceso");
    if (intento !== versionVista) return;
    if (!usuario?.activo) throw new Error("La cuenta no está habilitada.");
    estado.usuario = usuario;
    estado.previa = false;
    formulario.reset();
    // Se cierra la sesión al vencer. No existe renovación automática en este backend.
    vencimiento = setTimeout(
      () => salir("Tu sesión venció. Volvé a ingresar."),
      Math.min(Date.parse(acceso.expiracion) - Date.now(), 2147483647),
    );
    await cargarContexto();
    navegar("inicio");
  } catch (problema) {
    if (intento !== versionVista) return;
    clearTimeout(vencimiento);
    establecerToken(null);
    estado.usuario = null;
    error.textContent = problema.message;
    error.hidden = false;
  } finally {
    if (formulario.isConnected) {
      boton.disabled = false;
      boton.innerHTML = `Ingresar al sistema ${icono("flecha")}`;
    }
  }
}

async function cargarContexto({ institucionId, periodoId } = {}) {
  estado.instituciones = estado.previa
    ? [institucionEjemplo]
    : await solicitar("/instituciones");
  estado.institucion =
    estado.instituciones.find(
      (i) => i.id === (institucionId ?? estado.institucion?.id),
    ) ??
    estado.instituciones[0] ??
    null;
  if (!estado.institucion) {
    estado.periodos = [];
    estado.periodo = null;
    estado.aulas = [];
    estado.turnos = [];
    estado.ofertas = [];
    estado.turnosSolicitud = [];
    return;
  }
  const ruta = `/instituciones/${encodeURIComponent(estado.institucion.id)}`;
  const [periodos, aulas, turnos] = estado.previa
    ? [periodosEjemplo, aulasEjemplo, turnosEjemplo]
    : await Promise.all([
        solicitar(`${ruta}/periodos`),
        solicitar(`${ruta}/aulas`),
        solicitar(`${ruta}/turnos`),
      ]);
  estado.periodos = periodos;
  estado.aulas = aulas;
  estado.turnos = turnos;
  estado.periodo =
    periodos.find((p) => p.id === (periodoId ?? estado.periodo?.id)) ??
    periodos[0] ??
    null;
  estado.ofertas = !estado.periodo
    ? []
    : estado.previa
      ? ofertasEjemplo
      : await solicitar(
          `/periodos/${encodeURIComponent(estado.periodo.id)}/ofertas`,
        );
  if (estado.previa) estado.turnosSolicitud = turnos;
  else if (estado.periodo?.versionOfertaId) {
    const version = await solicitar(
      `/versiones-configuracion/${encodeURIComponent(estado.periodo.versionOfertaId)}`,
    );
    if (
      version.formato !== 1 ||
      version.tipo !== "Oferta" ||
      !Array.isArray(version.datos)
    )
      throw new Error(
        "La oferta publicada utiliza un formato que este frontend todavía no interpreta.",
      );
    const disponibles = version.datos
      .filter((o) => o.Disponible ?? o.disponible)
      .map((o) => o.TurnoId ?? o.turnoId);
    estado.turnosSolicitud = turnos.filter((t) => disponibles.includes(t.id));
  } else estado.turnosSolicitud = [];
}
contexto.recargarContexto = cargarContexto;

function estructura(ruta) {
  const pagina = ruta.split("/")[0];
  const grupos = [
    { nombre: "Tu espacio", elementos: [["inicio", "inicio", "Inicio"]] },
    {
      nombre: "Inscripciones",
      elementos: [
        ["solicitudes", "solicitudes", "Solicitudes"],
        ["domicilios", "ubicacion", "Domicilios y distancias"],
        ["documentacion", "lista", "Documentación"],
        ["duplicados", "personas", "Posibles duplicados"],
      ],
    },
    {
      nombre: "Configuración",
      elementos: [
        ["institucion", "jardin", "Mi institución"],
        ["periodos", "calendario", "Períodos"],
        ["oferta", "aulas", "Aulas, turnos y oferta"],
      ],
    },
    {
      nombre: "Organización",
      elementos: [
        ["vacantes", "personas", "Vacantes y admisión"],
        ["secciones", "aulas", "Secciones"],
        ["reportes", "lista", "Resultados e historial"],
        ...(esRoot() ? [["usuarios", "personas", "Usuarios y permisos"]] : []),
      ],
    },
  ];
  const activo =
    pagina === "solicitud" || pagina === "nueva-solicitud"
      ? "solicitudes"
      : pagina;
  const opciones = (items, valor, vacia) =>
    items.length
      ? items
          .map(
            (i) =>
              `<option value="${escapar(i.id)}" ${i.id === valor ? "selected" : ""}>${escapar(i.nombre ?? `Ciclo ${i.ciclo}`)}</option>`,
          )
          .join("")
      : `<option>${vacia}</option>`;
  raiz.innerHTML = `<div class="sistema"><aside class="menu" id="menu-principal" aria-label="Navegación principal"><a href="#inicio" class="menu__marca" aria-label="Jardín, inicio">${marca()}</a><nav>${grupos.map((g) => `<p class="menu__grupo">${g.nombre}</p>${g.elementos.map(([destino, dibujo, nombre]) => `<a class="menu__enlace ${activo === destino ? "menu__enlace--activo" : ""}" href="#${destino}" ${activo === destino ? 'aria-current="page"' : ""}>${icono(dibujo)}${nombre}</a>`).join("")}`).join("")}</nav><div class="menu__nota">Cada inscripción es<br>un nuevo comienzo.</div><div class="menu__pie"><a class="menu__enlace" href="#ayuda">${icono("ayuda")}Ayuda y estado del sistema</a><button class="boton boton--texto boton--ancho" data-accion="salir">${icono("salir")}${estado.previa ? "Salir de la vista previa" : "Cerrar sesión"}</button></div></aside><div class="sistema__principal"><header class="cabecera"><div class="cabecera__contexto"><button class="boton boton--texto boton--icono cabecera__menu" data-accion="menu" aria-controls="menu-principal" aria-expanded="false" aria-label="Abrir menú">${icono("lista")}</button><div class="cabecera__selector"><label for="institucion-activa">Institución</label><select id="institucion-activa" ${estado.instituciones.length ? "" : "disabled"}>${opciones(estado.instituciones, estado.institucion?.id, "Sin institución")}</select></div><div class="cabecera__separador"></div><div class="cabecera__selector"><label for="periodo-activo">Período de inscripción</label><select id="periodo-activo" ${estado.periodos.length ? "" : "disabled"}>${opciones(estado.periodos, estado.periodo?.id, "Sin período")}</select></div></div><div class="cabecera__cuenta"><div><p class="cabecera__nombre">${escapar(estado.usuario?.nombre)}</p><p class="cabecera__rol">${escapar(rolActual())}</p></div><span class="avatar">${escapar(iniciales(estado.usuario?.nombre))}</span></div></header>${estado.previa ? `<div class="aviso aviso--previa" role="status">${icono("ojo")}Vista previa · Todos los datos son ficticios. Los cambios no se guardan.<a href="#acceso" data-accion="salir">Iniciar sesión real</a></div>` : ""}<main class="contenido" id="contenido" tabindex="-1"><p class="contenido__miga">Mi espacio <span aria-hidden="true">/</span> ${escapar(titulos[pagina] ?? "Ficha de solicitud")}</p><div id="vista"><div class="cargando" role="status"><div class="cargando__rueda"></div><p>Cargando información…</p></div></div><footer class="contenido__pie"><span>Jardín · Gestión de inscripciones</span><span>${estado.previa ? "Primera versión · Vista previa" : "Primera versión · Entorno de desarrollo"}</span></footer></main></div></div>`;
}

async function cargarVista() {
  cerrarDialogo();
  if (!estado.usuario) {
    presentarAcceso();
    return;
  }
  let ruta = location.hash.slice(1) || "inicio";
  if (ruta === "acceso") {
    salir();
    return;
  }
  const intento = ++versionVista;
  estructura(ruta);
  document.title = `${titulos[ruta.split("/")[0]] ?? "Solicitud"} · Jardín`;
  try {
    const html = await presentarPagina(ruta, contexto);
    if (intento !== versionVista) return;
    document.getElementById("vista").innerHTML = html;
    document.getElementById("contenido").focus({ preventScroll: true });
  } catch (error) {
    if (intento !== versionVista) return;
    document.getElementById("vista").innerHTML = vacio({
      titulo: "No pudimos cargar esta página",
      descripcion: error.message,
      accion:
        '<button class="boton" data-accion="recargar">Intentar nuevamente</button>',
      dibujo: "alerta",
    });
  }
}

async function cambiarContexto(tipo, id) {
  if (
    estado.borrador &&
    !confirm(
      "Hay un registro de solicitud en curso. ¿Querés cambiar de contexto? Conservá el identificador del aspirante antes de continuar.",
    )
  ) {
    document.getElementById("institucion-activa").value =
      estado.institucion?.id ?? "";
    document.getElementById("periodo-activo").value = estado.periodo?.id ?? "";
    return;
  }
  versionVista++;
  cerrarDialogo();
  contexto.operacionOrganizacion = null;
  contexto.candidatosOferta = null;
  estado.pagina = 1;
  estado.filtros = {};
  estado.borrador = null;
  estado.borradores = [];
  const anterior = {
    instituciones: estado.instituciones,
    institucion: estado.institucion,
    periodos: estado.periodos,
    periodo: estado.periodo,
    aulas: estado.aulas,
    turnos: estado.turnos,
    ofertas: estado.ofertas,
    turnosSolicitud: estado.turnosSolicitud,
  };
  try {
    await cargarContexto(
      tipo === "institucion" ? { institucionId: id } : { periodoId: id },
    );
    await cargarVista();
  } catch (error) {
    if (!estado.usuario) return;
    Object.assign(estado, anterior);
    await cargarVista();
    notificar(error.message, true);
  }
}

document.addEventListener("click", async (evento) => {
  const elemento = evento.target.closest("[data-accion]");
  if (!elemento) return;
  evento.preventDefault();
  const accion = elemento.dataset.accion;
  if (accion === "salir") {
    salir();
    return;
  }
  if (accion === "mostrar-clave") {
    const clave = document.getElementById("clave-acceso");
    const mostrar = clave.type === "password";
    clave.type = mostrar ? "text" : "password";
    elemento.setAttribute(
      "aria-label",
      mostrar ? "Ocultar contraseña" : "Mostrar contraseña",
    );
    elemento.setAttribute("aria-pressed", String(mostrar));
    return;
  }
  if (accion === "alternar-clave") {
    const clave = document.getElementById(elemento.dataset.target);
    if (!clave) return;
    const mostrar = clave.type === "password";
    clave.type = mostrar ? "text" : "password";
    elemento.setAttribute(
      "aria-label",
      mostrar ? "Ocultar contraseña" : "Mostrar contraseña",
    );
    elemento.setAttribute("aria-pressed", String(mostrar));
    return;
  }
  if (accion === "vista-previa") {
    versionVista++;
    clearTimeout(vencimiento);
    establecerToken(null);
    estado.previa = true;
    estado.usuario = {
      nombre: "Marina Ejemplo",
      roles: ["Directivo"],
      accesos: [],
    };
    await cargarContexto();
    navegar("inicio");
    return;
  }
  if (accion === "cerrar-dialogo") {
    cerrarDialogo();
    return;
  }
  if (accion === "menu") {
    const abierto = document
      .getElementById("menu-principal")
      .classList.toggle("menu--abierto");
    elemento.setAttribute("aria-expanded", String(abierto));
    return;
  }
  if (accion === "recargar") {
    await cargarVista();
    return;
  }
  try {
    await atenderAccion(elemento, contexto);
  } catch (error) {
    notificar(error.message, true);
  }
});
document.addEventListener("submit", async (evento) => {
  if (evento.target.id === "formulario-acceso") {
    evento.preventDefault();
    await entrar(evento.target);
    return;
  }
  if (!evento.target.matches("[data-formulario]")) return;
  evento.preventDefault();
  await atenderFormulario(evento.target, contexto);
});
document.addEventListener("input", (evento) => {
  if (evento.target.matches("[data-clave-inicial], [data-clave-confirmacion]"))
    actualizarIndicadoresClave(evento.target.form);
});
document.addEventListener("change", (evento) => {
  if (evento.target.id === "institucion-activa")
    cambiarContexto("institucion", evento.target.value);
  if (evento.target.id === "periodo-activo")
    cambiarContexto("periodo", evento.target.value);
});
window.addEventListener("hashchange", cargarVista);
presentarAcceso();
