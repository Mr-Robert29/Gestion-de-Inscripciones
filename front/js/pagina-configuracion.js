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
  const periodosVigentes = ctx.estado.periodos.filter(
    (periodo) => periodo.estado !== "RecepcionCerrada",
  );
  const periodosCerrados = ctx.estado.periodos.filter(
    (periodo) => periodo.estado === "RecepcionCerrada",
  );
  const tablaPeriodos = (lista) =>
    `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Ciclo</th><th>Inicio previsto</th><th>Fin previsto</th><th>Estado</th><th>Configuración</th><th>Acción</th></tr></thead><tbody>${lista.map((p) => `<tr><td><strong>${e(p.ciclo)}</strong></td><td>${fecha(p.inicio)}</td><td>${fecha(p.fin)}</td><td>${etiqueta(p.estado)}</td><td>${p.versionReglasId && p.versionOfertaId ? etiqueta("Asignada") : etiqueta("Pendiente")}</td><td>${ctx.puedeEditar() && p.estado !== "RecepcionCerrada" ? `<button class="boton boton--secundario boton--pequeno" data-accion="recepcion" data-id="${e(p.id)}">${p.estado === "Preparado" ? "Abrir recepción" : "Cerrar recepción"}</button>` : '<span class="tabla__detalle">Sin acciones de recepción</span>'}</td></tr>`).join("")}</tbody></table></div>`;
  const cantidadPorCiclo = new Map();
  for (const periodo of ctx.estado.periodos)
    cantidadPorCiclo.set(periodo.ciclo, (cantidadPorCiclo.get(periodo.ciclo) ?? 0) + 1);
  const ciclosDuplicados = [...cantidadPorCiclo]
    .filter(([, cantidad]) => cantidad > 1)
    .map(([ciclo]) => ciclo)
    .sort((a, b) => a - b);
  const avisoDuplicados = ciclosDuplicados.length
    ? aviso(
        `Hay más de un período para los ciclos <strong>${ciclosDuplicados.map(e).join(", ")}</strong>. Se conservan los registros existentes para no afectar sus solicitudes; a partir de ahora no se podrán crear más períodos para esos ciclos.`,
        "aviso",
      )
    : "";
  const periodo = ctx.estado.periodo;
  const tieneReglas = Boolean(periodo?.versionReglasId);
  const tieneOferta = Boolean(periodo?.versionOfertaId);
  const versionesOferta = new Map((ctx.estado.ofertasVersionadas ?? []).map((oferta) => [oferta.id, oferta]));
  const cambiosOfertaPendientes = Boolean(periodo && tieneReglas && ctx.estado.ofertas.length && (
    ctx.estado.ofertas.length !== versionesOferta.size ||
    ctx.estado.ofertas.some((oferta) => versionesOferta.get(oferta.id)?.revision !== oferta.revision)
  ));
  const accionPublicarOferta = cambiosOfertaPendientes
    ? '<button class="boton boton--secundario" data-accion="publicar-oferta-periodo">Aplicar cambios pendientes</button>'
    : '<p class="tarjeta__descripcion">Las ofertas se publican automáticamente al guardarlas.</p>';
  const configuracionCiclo = !periodo
    ? '<p class="tarjeta__descripcion">Seleccioná un período para consultar o preparar su configuración.</p>'
    : `<div class="tarjeta__cuerpo"><p class="tarjeta__descripcion">Reglas: ${etiqueta(tieneReglas ? "Configuradas" : "Pendientes")} &nbsp; Oferta: ${etiqueta(tieneOferta ? "Publicada" : "Pendiente")}</p>${tieneReglas ? `<p><strong>Fecha de corte del ciclo:</strong> ${fecha(ctx.estado.fechaCortePeriodo)}</p>` : '<p class="tarjeta__descripcion">La fecha de corte se configura para este ciclo y se usa para calcular las edades de sus solicitudes.</p>'}<div class="formulario__acciones">${ctx.puedeEditar() ? tieneReglas && tieneOferta ? `<button class="boton boton--secundario" data-accion="editar-fecha-corte">Cambiar fecha de corte</button>${accionPublicarOferta}` : tieneReglas ? cambiosOfertaPendientes ? accionPublicarOferta : '<p class="tarjeta__descripcion">Agregá y guardá una oferta para publicarla automáticamente en este ciclo.</p>' : `<button class="boton" data-accion="preparar-ciclo" ${ctx.estado.ofertas.length ? "" : "disabled"}>Preparar configuración inicial</button>` : ""}</div>${!ctx.estado.ofertas.length ? '<p class="formulario__nota">Agregá al menos una oferta al período antes de publicarla.</p>' : ""}</div>`;
  return (
    encabezado({
      titulo: "Períodos de inscripción",
      descripcion: "Organizá cada ciclo lectivo y su recepción de solicitudes.",
      accion: ctx.puedeEditar()
        ? botonNuevo({ texto: "Crear período", accion: "nuevo-periodo" })
        : "",
    }) +
    `<section class="tarjeta"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Períodos vigentes</h2></div>${periodosVigentes.length ? tablaPeriodos(periodosVigentes) : vacio({ titulo: "No hay períodos vigentes", descripcion: "Los períodos cerrados se conservan en el historial. Creá un nuevo ciclo cuando quieras preparar otra inscripción.", dibujo: "calendario" })}</section>${periodosCerrados.length ? `<details class="tarjeta tarjeta--separada periodos__historial"><summary>Historial de períodos</summary><div class="tarjeta__cuerpo"><p class="tarjeta__descripcion">Períodos cerrados que se conservan para consultar sus solicitudes y configuraciones anteriores.</p>${avisoDuplicados}${tablaPeriodos(periodosCerrados)}</div></details>` : ""}<section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Configuración del ciclo ${e(periodo?.ciclo ?? "seleccionado")}</h2></div>${configuracionCiclo}</section>`
  );
}
export function oferta(ctx) {
  const { aulas, turnos, ofertas } = ctx.estado;
  const catalogo = ctx.estado.ofertasInstitucionales ?? [];
  const combinacionesEnPeriodo = new Set(ofertas.map((oferta) => `${oferta.aulaId}:${oferta.turnoId}`));
  const ofertasCatalogo = catalogo.length
    ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Aula</th><th>Turno</th><th>Capacidad de referencia</th><th>Referencia</th><th>Acción</th></tr></thead><tbody>${catalogo.map((plantilla) => {
        const aula = aulas.find((item) => item.id === plantilla.aulaId)?.nombre ?? "Aula";
        const turno = turnos.find((item) => item.id === plantilla.turnoId)?.nombre ?? "Turno";
        const agregada = combinacionesEnPeriodo.has(`${plantilla.aulaId}:${plantilla.turnoId}`);
        const agregar = ctx.estado.periodo
          ? agregada
            ? '<span class="tabla__detalle">Ya agregada</span>'
            : `<button class="boton boton--secundario boton--icono" data-accion="agregar-oferta-periodo" data-id="${e(plantilla.id)}" title="Agregar al ciclo ${e(ctx.estado.periodo.ciclo)}" aria-label="Agregar ${e(aula)} · ${e(turno)} al ciclo ${e(ctx.estado.periodo.ciclo)}">+</button>`
          : '<span class="tabla__detalle">Seleccioná un período</span>';
        return `<tr><td>${e(aula)}</td><td>${e(turno)}</td><td>${numero(plantilla.capacidad)}</td><td>${e(plantilla.referenciaCapacidad || "Sin referencia informada")}</td><td><div style="display:flex;align-items:center;gap:.5rem">${ctx.puedeEditar() ? `<button class="boton boton--secundario boton--pequeno" data-accion="editar-oferta-institucional" data-id="${e(plantilla.id)}">Editar</button>${ctx.estado.periodo ? agregar : ""}` : '<span class="tabla__detalle">Solo lectura</span>'}</div></td></tr>`;
      }).join("")}</tbody></table></div>`
    : vacio({
        titulo: "Todavía no hay ofertas reutilizables",
        descripcion: "Creá una combinación de aula y turno una sola vez. Después podrás agregarla a los ciclos que la necesiten.",
        dibujo: "aulas",
        accion: ctx.puedeEditar() ? '<button class="boton boton--secundario" data-accion="nueva-oferta-institucional">Crear oferta reutilizable</button>' : "",
      });
  const tablaPeriodo = ofertas.length
    ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Aula</th><th>Turno</th><th>Capacidad</th><th>Disponibilidad</th><th>Acción</th></tr></thead><tbody>${ofertas.map((o) => `<tr><td>${e(aulas.find((a) => a.id === o.aulaId)?.nombre ?? "Aula")}</td><td>${e(turnos.find((t) => t.id === o.turnoId)?.nombre ?? "Turno")}</td><td>${numero(o.capacidad)}</td><td>${etiqueta(o.disponible ? "Habilitada" : "Deshabilitada", o.disponible ? "" : "neutra")}</td><td>${ctx.puedeEditar() ? `<button class="boton boton--secundario boton--pequeno" data-accion="editar-oferta-periodo" data-id="${e(o.id)}">Ajustar</button>` : '<span class="tabla__detalle">Solo lectura</span>'}</td></tr>`).join("")}</tbody></table></div>`
    : vacio({
        titulo: "Todavía no hay ofertas para este ciclo",
        descripcion: "Agregá al período las combinaciones reutilizables que quieras habilitar. Cada ciclo conserva su propia capacidad y ocupación.",
        dibujo: "aulas",
      });
  return (
    encabezado({
      titulo: "Aulas, turnos y oferta",
      descripcion: "Administrá combinaciones reutilizables y elegí cuáles habilitar en cada ciclo.",
      accion:
        ctx.puedeEditar()
          ? botonNuevo({ texto: "Crear oferta reutilizable", accion: "nueva-oferta-institucional" })
          : "",
    }) +
    aviso(
      "La capacidad neta inicial descuenta las ocupaciones declaradas. No es el cálculo final de vacantes ni garantiza un ingreso.",
    ) +
    `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Aulas", descripcion: "Espacios de tu institución.", accion: ctx.puedeEditar() ? '<button class="boton boton--texto boton--pequeno" data-accion="nueva-aula">+ Agregar</button>' : "", cuerpo: aulas.length ? aulas.map((a) => `<div class="acceso-rapido"><strong class="acceso-rapido__titulo">${e(a.nombre)}</strong></div>`).join("") : '<p class="formulario__nota">Todavía no hay aulas registradas.</p>' })}${tarjeta({ titulo: "Turnos", descripcion: "Horarios disponibles.", accion: ctx.puedeEditar() ? '<button class="boton boton--texto boton--pequeno" data-accion="nuevo-turno">+ Agregar</button>' : "", cuerpo: turnos.length ? turnos.map((t) => `<div class="acceso-rapido"><strong class="acceso-rapido__titulo">${e(t.nombre)}</strong></div>`).join("") : '<p class="formulario__nota">Todavía no hay turnos registrados.</p>' })}</div><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">Ofertas reutilizables de la institución</h2><p class="tarjeta__descripcion">Estas combinaciones no pertenecen a un ciclo hasta que las agregás.</p></div></div>${ofertasCatalogo}</section><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">Oferta del ciclo ${e(ctx.estado.periodo?.ciclo ?? "seleccionado")}</h2><p class="tarjeta__descripcion">Cada ciclo tiene una copia independiente. Después de modificarla, publicá los cambios desde Períodos para que se usen en las solicitudes.</p></div></div>${tablaPeriodo}</section>`
  );
}
