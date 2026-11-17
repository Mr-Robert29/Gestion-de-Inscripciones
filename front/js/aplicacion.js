import {
  solicitar,
  establecerToken,
  obtenerDireccionApi,
} from "./api.js";
import {
  escapar,
  iniciales,
  icono,
  marca,
  jardinIlustrado,
  campo,
  campoClave,
  notificar,
  vacio,
  etiqueta,
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
import { validarClave } from "./validacion-clave.js";
import { edadesDeReglas } from "./reglas-edad.js";

const raiz = document.getElementById("aplicacion");
const estado = {
  usuario: null,
  previa: false,
  instituciones: [],
  institucion: null,
  periodos: [],
  periodo: null,
  fechaCortePeriodo: null,
  edadesAdmitidasPeriodo: null,
  aulas: [],
  turnos: [],
  ofertasInstitucionales: [],
  ofertas: [],
  ofertasVersionadas: [],
  ofertasPublicadas: [],
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

async function mostrarDatosUsuario(usuarioId) {
  const usuario = contexto.organizacion?.usuarios?.find((x) => x.id === usuarioId);
  if (!usuario) {
    notificar("No se encontró la cuenta. Actualizá la pantalla e intentá de nuevo.", true);
    return;
  }
  const identidad = await contexto.api(`/usuarios/${encodeURIComponent(usuarioId)}/dni`);
  const rolesGlobales = usuario.rolesGlobales?.length
    ? usuario.rolesGlobales.map((rol) => etiqueta(rol)).join(" ")
    : '<span class="tabla__detalle">Sin rol global asignado</span>';
  const accesos = usuario.accesos?.length
    ? `<ul class="usuarios__datos-accesos">${usuario.accesos.map((acceso) => {
        const institucion = estado.instituciones.find((x) => x.id === acceso.institucionId);
        return `<li><div><span class="usuarios__dato-etiqueta">Institución</span><strong>${escapar(institucion?.nombre ?? "Institución no disponible")}</strong></div><div><span class="usuarios__dato-etiqueta">Rol en esta institución</span>${etiqueta(acceso.rol)}</div></li>`;
      }).join("")}</ul>`
    : '<p class="tabla__detalle">Sin instituciones asignadas</p>';
  abrirDialogo({
    titulo: "Datos de la cuenta",
    descripcion: `${usuario.nombre} · ${usuario.correoElectronico}`,
    contenido: `<div class="usuarios__datos"><section class="usuarios__dato"><h3>DNI completo</h3><p>${escapar(identidad.dni ?? "Sin DNI vinculado")}</p></section><section class="usuarios__dato"><h3>Rol global</h3><div>${rolesGlobales}</div></section><section class="usuarios__dato usuarios__dato--ancho"><h3>Accesos institucionales</h3>${accesos}</section></div><div class="formulario__acciones"><button class="boton" data-accion="cerrar-dialogo">Cerrar</button></div>`,
  });
}

function actualizarFiltroCuentas() {
  const filtro = document.querySelector("[data-filtro-cuentas]")?.value ?? "activo";
  const busqueda = (document.querySelector("[data-buscar-cuentas]")?.value ?? "")
    .trim().toLocaleLowerCase("es");
  const filas = [...document.querySelectorAll("tr[data-estado-cuenta]")];
  for (const fila of filas) {
    const coincideEstado = filtro === "todas" || fila.dataset.estadoCuenta === filtro;
    const coincideBusqueda = !busqueda || fila.textContent.toLocaleLowerCase("es").includes(busqueda);
    fila.hidden = !(coincideEstado && coincideBusqueda);
  }
  const visibles = filas.filter((fila) => !fila.hidden).length;
  const vacio = document.querySelector("[data-cuentas-vacio]");
  if (vacio) {
    vacio.hidden = visibles > 0;
    vacio.textContent = busqueda
      ? "No se encontraron cuentas con esos datos para el filtro seleccionado."
      : filtro === "activo"
        ? "No hay cuentas activas para mostrar con este filtro."
        : filtro === "inactivo"
          ? "No hay cuentas inactivas para mostrar con este filtro."
          : "No hay cuentas para mostrar.";
  }
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
    fechaCortePeriodo: null,
    edadesAdmitidasPeriodo: null,
    aulas: [],
    turnos: [],
    ofertasInstitucionales: [],
    ofertas: [],
    ofertasVersionadas: [],
    ofertasPublicadas: [],
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
  raiz.innerHTML = `<main class="acceso" id="contenido"><section class="acceso__presentacion"><div>${marca()}</div><div class="acceso__mensaje"><p class="acceso__epigrafe">Cada comienzo cuenta</p><h2 class="acceso__titulo">Un lugar para<br>crecer.<br><em>Una gestión<br>más simple.</em></h2><p class="acceso__descripcion">Acompañá cada inscripción, organizá tu jardín y dedicá más tiempo a lo que importa.</p></div><div class="acceso__ilustracion">${jardinIlustrado()}</div><p class="acceso__pie">Un espacio de trabajo para tu comunidad educativa.</p></section><section class="acceso__formulario" aria-label="Inicio de sesión"><div class="acceso__panel"><p class="acceso__saludo">Bienvenido a tu jardín</p><h1>Iniciá sesión</h1><p class="acceso__instruccion">Ingresá con tu cuenta para continuar.</p><form class="formulario" id="formulario-acceso">${campo({ nombre: "correo", etiqueta: "Correo electrónico", tipo: "email", obligatorio: true, atributos: 'autocomplete="username" placeholder="nombre@institucion.edu.ar"' })}<div class="campo"><label class="campo__etiqueta" for="clave-acceso">Contraseña *</label><div class="acceso__clave"><input id="clave-acceso" class="campo__control" type="password" name="clave" required autocomplete="current-password" placeholder="Ingresá tu contraseña"><button type="button" class="boton boton--texto boton--icono acceso__mostrar" data-accion="mostrar-clave" aria-label="Mostrar contraseña" aria-pressed="false">${icono("ojo")}</button></div></div><p class="formulario__error" data-error role="alert" hidden></p><button class="boton boton--ancho" type="submit">Ingresar al sistema ${icono("flecha")}</button></form><p class="acceso__seguridad">${icono("candado")} Acceso exclusivo para personal autorizado</p></div></section></main>`;
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
    if (acceso.debeCambiarClave) {
      formulario.reset();
      presentarCambioClave();
      vencimiento = setTimeout(
        () => salir("Tu sesión venció. Volvé a ingresar."),
        Math.min(Date.parse(acceso.expiracion) - Date.now(), 2147483647),
      );
      return;
    }
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

function presentarCambioClave() {
  raiz.innerHTML = `<main class="acceso" id="contenido"><section class="acceso__formulario" aria-label="Cambio obligatorio de contraseña"><div class="acceso__panel"><p class="acceso__saludo">Primer ingreso</p><h1>Elegí una contraseña propia</h1><p class="acceso__instruccion">La contraseña temporal que te asignaron ya no se usará después de este cambio.</p><form class="formulario" id="formulario-cambio-clave">${campo({ nombre: "claveActual", etiqueta: "Contraseña temporal", tipo: "password", obligatorio: true, atributos: 'autocomplete="current-password"' })}${campoClave({ nombre: "claveNueva", etiqueta: "Nueva contraseña", obligatorio: true })}${campoClave({ nombre: "confirmarClave", etiqueta: "Confirmar nueva contraseña", obligatorio: true, confirmacion: true })}<p class="formulario__error" data-error role="alert" hidden></p><button class="boton boton--ancho" type="submit">Guardar contraseña nueva ${icono("flecha")}</button></form><p class="acceso__seguridad">Por seguridad, este paso es obligatorio para continuar.</p></div></section></main>`;
}

async function cambiarClaveInicial(formulario) {
  const boton = formulario.querySelector('button[type="submit"]');
  const error = formulario.querySelector("[data-error]");
  const datos = Object.fromEntries(new FormData(formulario));
  boton.disabled = true;
  error.hidden = true;
  try {
    validarClave(datos.claveNueva, datos.confirmarClave);
    const acceso = await solicitar("/Autenticacion/cambiar-clave", {
      metodo: "POST",
      datos: { claveActual: datos.claveActual, claveNueva: datos.claveNueva },
    });
    if (!acceso?.token || acceso.debeCambiarClave)
      throw new Error("No se pudo completar el cambio obligatorio de contraseña.");
    establecerToken(acceso.token);
    const usuario = await solicitar("/Autenticacion/mi-acceso");
    if (!usuario?.activo) throw new Error("La cuenta no está habilitada.");
    estado.usuario = usuario;
    vencimiento = setTimeout(
      () => salir("Tu sesión venció. Volvé a ingresar."),
      Math.min(Date.parse(acceso.expiracion) - Date.now(), 2147483647),
    );
    await cargarContexto();
    navegar("inicio");
    notificar("La contraseña se actualizó. Ya podés usar el sistema.");
  } catch (problema) {
    if (formulario.isConnected) {
      error.textContent = problema.message;
      error.hidden = false;
    }
  } finally {
    if (boton.isConnected) boton.disabled = false;
  }
}

function claveContextoPersistido() {
  const usuarioId = estado.usuario?.usuarioId;
  if (estado.previa || !usuarioId) return null;
  return `jardin:contexto:${JSON.stringify([obtenerDireccionApi(), usuarioId])}`;
}

function leerContextoPersistido() {
  try {
    const clave = claveContextoPersistido();
    return clave ? JSON.parse(localStorage.getItem(clave) ?? "null") : null;
  } catch {
    return null;
  }
}

function guardarContextoPersistido() {
  try {
    const clave = claveContextoPersistido();
    if (clave) {
      localStorage.setItem(
        clave,
        JSON.stringify({
          institucionId: estado.institucion?.id,
          periodoId: estado.periodo?.id,
        }),
      );
    }
  } catch {
    // El contexto sigue disponible durante esta sesión.
  }
}

async function cargarContexto({ institucionId, periodoId } = {}) {
  const contextoGuardado = leerContextoPersistido();
  estado.instituciones = estado.previa
    ? [institucionEjemplo]
    : await solicitar("/instituciones");
  estado.institucion =
    estado.instituciones.find(
      (i) => i.id === (institucionId ?? estado.institucion?.id ?? contextoGuardado?.institucionId),
    ) ??
    estado.instituciones[0] ??
    null;
  if (!estado.institucion) {
    estado.periodos = [];
    estado.periodo = null;
    estado.fechaCortePeriodo = null;
    estado.edadesAdmitidasPeriodo = null;
    estado.aulas = [];
    estado.turnos = [];
    estado.ofertasInstitucionales = [];
    estado.ofertas = [];
    estado.ofertasVersionadas = [];
    estado.ofertasPublicadas = [];
    estado.turnosSolicitud = [];
    return;
  }
  const ruta = `/instituciones/${encodeURIComponent(estado.institucion.id)}`;
  const [periodos, aulas, turnos, ofertasInstitucionales] = estado.previa
    ? [
        periodosEjemplo,
        aulasEjemplo,
        turnosEjemplo,
        ofertasEjemplo.map((oferta) => ({
          id: oferta.id,
          institucionId: institucionEjemplo.id,
          aulaId: oferta.aulaId,
          turnoId: oferta.turnoId,
          capacidad: oferta.capacidad,
          referenciaCapacidad: oferta.referenciaCapacidad,
        })),
      ]
    : await Promise.all([
        solicitar(`${ruta}/periodos`),
        solicitar(`${ruta}/aulas`),
        solicitar(`${ruta}/turnos`),
        solicitar(`${ruta}/ofertas-reutilizables`),
      ]);
  estado.periodos = periodos;
  estado.aulas = aulas;
  estado.turnos = turnos;
  estado.ofertasInstitucionales = ofertasInstitucionales;
  estado.periodo =
    periodos.find((p) => p.id === (periodoId ?? estado.periodo?.id ?? contextoGuardado?.periodoId)) ??
    periodos[0] ??
    null;
  estado.fechaCortePeriodo = estado.previa ? "2027-06-30" : null;
  estado.edadesAdmitidasPeriodo = estado.previa ? [3, 4, 5] : null;
  estado.ofertas = !estado.periodo
    ? []
    : estado.previa
      ? ofertasEjemplo
      : await solicitar(
          `/periodos/${encodeURIComponent(estado.periodo.id)}/ofertas`,
        );
  if (estado.previa) {
    estado.turnosSolicitud = turnos;
    estado.ofertasVersionadas = ofertasEjemplo;
    estado.ofertasPublicadas = ofertasEjemplo.filter((oferta) => oferta.disponible);
  }
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
    estado.ofertasVersionadas = version.datos.map((oferta) => ({
        id: oferta.Id ?? oferta.id,
        revision: oferta.Revision ?? oferta.revision,
        aulaId: oferta.AulaId ?? oferta.aulaId,
        turnoId: oferta.TurnoId ?? oferta.turnoId,
        capacidad: oferta.Capacidad ?? oferta.capacidad,
        capacidadNetaInicial: oferta.CapacidadNetaInicial ?? oferta.capacidadNetaInicial ?? oferta.Capacidad ?? oferta.capacidad,
        ocupaciones: oferta.Ocupaciones ?? oferta.ocupaciones ?? [],
        disponible: oferta.Disponible ?? oferta.disponible,
      }));
    estado.ofertasPublicadas = estado.ofertasVersionadas
      .filter((oferta) => oferta.disponible)
      .filter((oferta) => oferta.aulaId && oferta.turnoId && oferta.capacidad > 0);
    const disponibles = estado.ofertasPublicadas.map((oferta) => oferta.turnoId);
    estado.turnosSolicitud = turnos.filter((t) => disponibles.includes(t.id));
  } else {
    estado.turnosSolicitud = [];
    estado.ofertasVersionadas = [];
    estado.ofertasPublicadas = [];
  }
  if (!estado.previa && estado.periodo?.versionReglasId) {
    const versionReglas = await solicitar(
      `/versiones-configuracion/${encodeURIComponent(estado.periodo.versionReglasId)}`,
    );
    if (
      versionReglas.formato !== 1 ||
      versionReglas.tipo !== "Reglas" ||
      !versionReglas.datos
    )
      throw new Error("Las reglas publicadas del período no tienen un formato compatible.");
    estado.fechaCortePeriodo = versionReglas.datos.FechaCorte ?? versionReglas.datos.fechaCorte ?? null;
    estado.edadesAdmitidasPeriodo = edadesDeReglas(versionReglas.datos);
  }
  guardarContextoPersistido();
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
        ...(esRoot() || rolActual() === "Directivo" ? [["personal", "personas", "Personal del jardín"]] : []),
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
  raiz.innerHTML = `<div class="sistema"><aside class="menu" id="menu-principal" aria-label="Navegación principal"><a href="#inicio" class="menu__marca" aria-label="Jardín, inicio">${marca()}</a><nav>${grupos.map((g) => `<p class="menu__grupo">${g.nombre}</p>${g.elementos.map(([destino, dibujo, nombre]) => `<a class="menu__enlace ${activo === destino ? "menu__enlace--activo" : ""}" href="#${destino}" ${activo === destino ? 'aria-current="page"' : ""}>${icono(dibujo)}${nombre}</a>`).join("")}`).join("")}</nav><div class="menu__nota">Cada inscripción es<br>un nuevo comienzo.</div><div class="menu__pie"><a class="menu__enlace ${activo === "tutorial" ? "menu__enlace--activo" : ""}" href="#tutorial" ${activo === "tutorial" ? 'aria-current="page"' : ""}>${icono("ayuda")}Tutorial de uso</a><a class="menu__enlace ${activo === "ayuda" ? "menu__enlace--activo" : ""}" href="#ayuda" ${activo === "ayuda" ? 'aria-current="page"' : ""}>${icono("ayuda")}Ayuda y estado del sistema</a><button class="boton boton--texto boton--ancho" data-accion="salir">${icono("salir")}${estado.previa ? "Salir de la vista previa" : "Cerrar sesión"}</button></div></aside><div class="sistema__principal"><header class="cabecera"><div class="cabecera__contexto"><button class="boton boton--texto boton--icono cabecera__menu" data-accion="menu" aria-controls="menu-principal" aria-expanded="false" aria-label="Abrir menú">${icono("lista")}</button><div class="cabecera__selector"><label for="institucion-activa">Institución</label><select id="institucion-activa" ${estado.instituciones.length ? "" : "disabled"}>${opciones(estado.instituciones, estado.institucion?.id, "Sin institución")}</select></div><div class="cabecera__separador"></div><div class="cabecera__selector"><label for="periodo-activo">Período de inscripción</label><select id="periodo-activo" ${estado.periodos.length ? "" : "disabled"}>${opciones(estado.periodos, estado.periodo?.id, "Sin período")}</select></div></div><div class="cabecera__cuenta"><div><p class="cabecera__nombre">${escapar(estado.usuario?.nombre)}</p><p class="cabecera__rol">${escapar(rolActual())}</p><p class="cabecera__correo" title="${escapar(estado.usuario?.correoElectronico)}">${escapar(estado.usuario?.correoElectronico)}</p></div><span class="avatar">${escapar(iniciales(estado.usuario?.nombre))}</span></div></header>${estado.previa ? `<div class="aviso aviso--previa" role="status">${icono("ojo")}Vista previa · Todos los datos son ficticios. Los cambios no se guardan.<a href="#acceso" data-accion="salir">Iniciar sesión real</a></div>` : ""}<main class="contenido" id="contenido" tabindex="-1"><p class="contenido__miga">Mi espacio <span aria-hidden="true">/</span> ${escapar(titulos[pagina] ?? "Ficha de solicitud")}</p><div id="vista"><div class="cargando" role="status"><div class="cargando__rueda"></div><p>Cargando información…</p></div></div><footer class="contenido__pie"><span>Jardín · Gestión de inscripciones</span><span>${estado.previa ? "Primera versión · Vista previa" : "Primera versión · Entorno de desarrollo"}</span></footer></main></div></div>`;
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
  if (accion === "ver-datos-usuario") {
    try {
      await mostrarDatosUsuario(elemento.dataset.id);
    } catch (error) {
      notificar(error.message, true);
    }
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
  if (accion === "usuarios-tab") {
    const seleccionada = elemento.dataset.panel;
    document.querySelectorAll("[data-usuarios-panel]").forEach((panel) => {
      panel.hidden = panel.dataset.usuariosPanel !== seleccionada;
    });
    document.querySelectorAll('[data-accion="usuarios-tab"]').forEach((pestana) => {
      const activa = pestana.dataset.panel === seleccionada;
      pestana.classList.toggle("pestanas__enlace--activo", activa);
      pestana.setAttribute("aria-selected", String(activa));
    });
    return;
  }
  if (elemento.closest("#dialogo") && accion.startsWith("org-")) cerrarDialogo();
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
  if (evento.target.id === "formulario-cambio-clave") {
    evento.preventDefault();
    await cambiarClaveInicial(evento.target);
    return;
  }
  if (!evento.target.matches("[data-formulario]")) return;
  evento.preventDefault();
  await atenderFormulario(evento.target, contexto);
});
document.addEventListener("change", (evento) => {
  const selecciones = {
    articulacionPedagogica: "articulacion",
    respuestaArticulacion: "articulacion",
    hermanosGrupoConviviente: "hermanos",
    respuestaHermanosGrupo: "hermanos",
    hijoPersonal: "personal",
    respuestaHijoPersonal: "personal",
  };
  const grupo = selecciones[evento.target.name];
  if (!grupo) return;
  const formulario = evento.target.closest("[data-formulario]");
  const detalle = formulario?.querySelector(`[data-prioridad-detalle="${grupo}"]`);
  if (!detalle) return;
  const mostrar = grupo === "articulacion"
    ? ["Si", "NoEstoySeguro"].includes(evento.target.value)
    : evento.target.value === "Si";
  detalle.hidden = !mostrar;
  const entrada = detalle.querySelector("input");
  if (entrada) {
    entrada.required = mostrar;
    if (!mostrar) entrada.value = "";
  }
});
document.addEventListener("input", (evento) => {
  if (evento.target.matches("[data-clave-inicial], [data-clave-confirmacion]"))
    actualizarIndicadoresClave(evento.target.form);
  if (evento.target.matches("[data-buscar-cuentas]")) actualizarFiltroCuentas();
  if (evento.target.matches("[data-motivo-otro]")) {
    const valorFinal = evento.target.form?.querySelector("[data-motivo-final]");
    if (valorFinal) valorFinal.value = evento.target.value.trim();
  }
});
document.addEventListener("change", (evento) => {
  if (evento.target.matches("[data-filtro-aceptacion-turno]")) {
    const filtro = evento.target.value;
    const filas = [...document.querySelectorAll(".propuesta__tabla tr[data-estado-cuenta]")];
    for (const fila of filas)
      fila.hidden = filtro !== "todas" && fila.dataset.estadoCuenta !== filtro;
    const visibles = filas.filter((fila) => !fila.hidden).length;
    const resumen = document.querySelector("[data-resumen-aceptacion-turno]");
    if (resumen)
      resumen.textContent = filtro === "todas"
        ? `${filas.filter((fila) => fila.dataset.estadoCuenta === "pendiente").length} pendientes de registrar`
        : `${visibles} aspirantes visibles`;
  }
  if (evento.target.matches("[data-motivo-seleccion]")) {
    const form = evento.target.form;
    const esOtro = evento.target.value === "__otro__";
    const contenedorOtro = form?.querySelector("[data-motivo-otro-contenedor]");
    const textoOtro = form?.querySelector("[data-motivo-otro]");
    const valorFinal = form?.querySelector("[data-motivo-final]");
    if (contenedorOtro) contenedorOtro.hidden = !esOtro;
    evento.target.setAttribute("aria-expanded", String(esOtro));
    if (textoOtro) {
      textoOtro.disabled = !esOtro;
      textoOtro.required = esOtro;
      if (esOtro) textoOtro.focus();
    }
    if (valorFinal)
      valorFinal.value = esOtro ? (textoOtro?.value.trim() ?? "") : evento.target.value;
    return;
  }
  if (evento.target.matches("[data-tipo-cuenta]")) {
    const institucional = evento.target.value === "institucional";
    const camposInstitucion = evento.target.form?.querySelector("[data-cuenta-institucional]");
    const notaRoot = evento.target.form?.querySelector("[data-cuenta-root]");
    if (camposInstitucion) {
      camposInstitucion.hidden = !institucional;
      camposInstitucion.disabled = !institucional;
    }
    if (notaRoot) notaRoot.hidden = institucional;
    return;
  }
  if (evento.target.matches("[data-filtro-cuentas]")) {
    actualizarFiltroCuentas();
    return;
  }
  if (evento.target.id === "institucion-activa")
    cambiarContexto("institucion", evento.target.value);
  if (evento.target.id === "periodo-activo")
    cambiarContexto("periodo", evento.target.value);
  if (evento.target.id === "filtro-seguimiento-domicilio") {
    const filtro = evento.target.value;
    const filas = [...document.querySelectorAll("tr[data-distancia-estado]")];
    for (const fila of filas) {
      const coincide = filtro === "todos"
        || (filtro === "domicilio-pendiente" && fila.dataset.domicilioEstado === "pendiente")
        || (filtro === "distancia-pendiente" && fila.dataset.distanciaEstado === "pendiente")
        || (filtro === "distancia-manual" && fila.dataset.distanciaEstado === "manual")
        || (filtro === "distancia-desactualizada" && fila.dataset.distanciaEstado === "desactualizada");
      fila.hidden = !coincide;
    }
    const visibles = filas.filter((fila) => !fila.hidden).length;
    const resumen = document.querySelector("[data-resultados-domicilio]");
    if (resumen) resumen.textContent = `${visibles} solicitudes visibles en esta página`;
  }
});
window.addEventListener("hashchange", cargarVista);
presentarAcceso();
