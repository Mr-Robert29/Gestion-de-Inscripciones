import {
  escapar as e,
  etiqueta,
  fecha,
  numero,
  aviso,
  vacio,
  direccionTexto,
} from "./interfaz.js";
import { encabezado, botonNuevo, tarjeta } from "./compartidos.js";
export function sinInstitucion(ctx) {
  return (
    encabezado({
      titulo: "Tu espacio de trabajo",
      descripcion:
        "Primero necesitamos una institución a la que puedas acceder.",
    }) +
    `<section class="tarjeta">${vacio({ titulo: "Todavía no tenés instituciones disponibles", descripcion: ctx.puedeCrearInstitucion() ? "Registrá tu jardín para comenzar a configurar las inscripciones." : "Solicitá al administrador que asigne una institución a tu cuenta.", accion: ctx.puedeCrearInstitucion() ? botonNuevo({ texto: "Registrar institución", accion: "nueva-institucion" }) : "", dibujo: "jardin" })}</section>`
  );
}
export function institucion(ctx) {
  const d = ctx.estado.institucion;
  return (
    encabezado({
      titulo: "Mi institución",
      descripcion: "Los datos que identifican a tu jardín.",
      accion: ctx.puedeCrearInstitucion()
        ? botonNuevo({
            texto: "Registrar otra institución",
            accion: "nueva-institucion",
          })
        : "",
    }) +
    `<div class="rejilla rejilla--principal">${tarjeta({ titulo: e(d.nombre), descripcion: "Información institucional y de contacto.", cuerpo: `<dl class="datos"><div><dt>Nombre</dt><dd>${e(d.nombre)}</dd></div><div><dt>Código institucional</dt><dd>${e(d.codigo || "No informado")}</dd></div><div><dt>Teléfono</dt><dd>${e(d.telefonoContacto)}</dd></div><div><dt>Correo electrónico</dt><dd>${e(d.correoElectronicoContacto || "No informado")}</dd></div><div class="campo--completo"><dt>Domicilio</dt><dd>${e(direccionTexto(d.domicilio))}</dd></div><div><dt>Ubicación</dt><dd>${d.coordenadas ? `${e(d.coordenadas.latitud)}, ${e(d.coordenadas.longitud)}` : "Coordenadas pendientes"}</dd></div><div><dt>Estado</dt><dd>${etiqueta(d.activa === false ? "Inactiva" : "Activa")}</dd></div></dl><hr class="separador">${ctx.puedeEditar() ? '<button class="boton boton--secundario" data-accion="editar-institucion">Editar datos del jardín</button>' : '<p class="formulario__nota">Tu cuenta tiene acceso de consulta a esta configuración.</p>'}` })}${tarjeta({ titulo: "La ubicación importa", cuerpo: `<p class="tarjeta__descripcion">Cuando cambia el domicilio o la ubicación, las mediciones anteriores pueden necesitar revisión.</p><hr class="separador">${aviso("La distancia se carga manualmente en esta primera versión. La localización automática se incorporará más adelante.", "aviso")}<a class="boton boton--texto" href="#domicilios">Revisar domicilios →</a>` })}</div>`
  );
}
export function periodos(ctx) {
  return (
    encabezado({
      titulo: "Períodos de inscripción",
      descripcion: "Organizá cada ciclo lectivo y su recepción de solicitudes.",
      accion: ctx.puedeEditar()
        ? botonNuevo({ texto: "Crear período", accion: "nuevo-periodo" })
        : "",
    }) +
    `<section class="tarjeta"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Períodos de tu institución</h2></div>${ctx.estado.periodos.length ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Ciclo</th><th>Inicio previsto</th><th>Fin previsto</th><th>Estado</th><th>Configuración</th><th>Acción</th></tr></thead><tbody>${ctx.estado.periodos.map((p) => `<tr><td><strong>${e(p.ciclo)}</strong></td><td>${fecha(p.inicio)}</td><td>${fecha(p.fin)}</td><td>${etiqueta(p.estado)}</td><td>${p.versionReglasId && p.versionOfertaId ? etiqueta("Asignada") : etiqueta("Pendiente")}</td><td>${ctx.puedeEditar() && p.estado !== "RecepcionCerrada" ? `<button class="boton boton--secundario boton--pequeno" data-accion="recepcion" data-id="${e(p.id)}">${p.estado === "Preparado" ? "Abrir recepción" : "Cerrar recepción"}</button>` : '<span class="tabla__detalle">Sin acciones de recepción</span>'}</td></tr>`).join("")}</tbody></table></div>` : vacio({ titulo: "Prepará el primer período", descripcion: "Creá un ciclo para organizar las solicitudes de tu institución.", dibujo: "calendario" })}</section><div class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Reglas y oferta del período seleccionado</h2></div><div class="tarjeta__cuerpo"><p class="tarjeta__descripcion">Las solicitudes necesitan una versión de reglas y una de oferta asignadas al período.</p><div class="formulario__acciones">${ctx.puedeEditar() && ctx.estado.periodo ? '<button class="boton boton--secundario" data-accion="preparar-versiones">Publicar y asignar versiones</button>' : ""}</div></div></div>`
  );
}
export function oferta(ctx) {
  const { aulas, turnos, ofertas } = ctx.estado;
  return (
    encabezado({
      titulo: "Aulas, turnos y oferta",
      descripcion: "Prepará los recursos disponibles para cada período.",
      accion:
        ctx.puedeEditar() && ctx.estado.periodo
          ? botonNuevo({ texto: "Agregar oferta", accion: "nueva-oferta" })
          : "",
    }) +
    aviso(
      "La capacidad neta inicial descuenta las ocupaciones declaradas. No es el cálculo final de vacantes ni garantiza un ingreso.",
    ) +
    `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Aulas", descripcion: "Espacios de tu institución.", accion: ctx.puedeEditar() ? '<button class="boton boton--texto boton--pequeno" data-accion="nueva-aula">+ Agregar</button>' : "", cuerpo: aulas.length ? aulas.map((a) => `<div class="acceso-rapido"><strong class="acceso-rapido__titulo">${e(a.nombre)}</strong></div>`).join("") : '<p class="formulario__nota">Todavía no hay aulas registradas.</p>' })}${tarjeta({ titulo: "Turnos", descripcion: "Horarios disponibles.", accion: ctx.puedeEditar() ? '<button class="boton boton--texto boton--pequeno" data-accion="nuevo-turno">+ Agregar</button>' : "", cuerpo: turnos.length ? turnos.map((t) => `<div class="acceso-rapido"><strong class="acceso-rapido__titulo">${e(t.nombre)}</strong></div>`).join("") : '<p class="formulario__nota">Todavía no hay turnos registrados.</p>' })}</div><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Oferta del período seleccionado</h2></div>${ofertas.length ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Aula</th><th>Turno</th><th>Capacidad</th><th>Ocupación inicial</th><th>Neta inicial</th><th>Disponibilidad</th></tr></thead><tbody>${ofertas.map((o) => `<tr><td>${e(aulas.find((a) => a.id === o.aulaId)?.nombre ?? "Aula")}</td><td>${e(turnos.find((t) => t.id === o.turnoId)?.nombre ?? "Turno")}</td><td>${numero(o.capacidad)}</td><td>${numero(o.ocupaciones.reduce((s, c) => s + c.cantidad, 0))}</td><td><strong>${numero(o.capacidadNetaInicial)}</strong></td><td>${etiqueta(o.disponible ? "Disponible" : "No disponible", o.disponible ? "" : "neutra")}</td></tr>`).join("")}</tbody></table></div>` : vacio({ titulo: "La oferta todavía está por preparar", descripcion: "Agregá una combinación de aula y turno con capacidad y ocupaciones iniciales.", dibujo: "aulas" })}</section>`
  );
}
