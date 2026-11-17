import {
  escapar as e,
  etiqueta,
  fecha,
  numero,
  direccionTexto,
} from "./interfaz.js";
import { encabezado, tarjeta, base, volver } from "./compartidos.js";
import { detalleEjemplo } from "./datos-previa.js";
const nombreCondicion = (tipo) => ({
  ArticulacionPedagogica: "Articulación pedagógica",
  ConsultaArticulacionPedagogica: "Consulta sobre articulación pedagógica",
  DeclaracionArticulacionNegativa: "Respuesta sobre articulación",
  HermanosYGruposConvivientes: "Hermanos o grupo conviviente",
  DeclaracionHermanosNegativa: "Respuesta sobre hermanos o grupo conviviente",
  HijosDelPersonal: "Hijo/a del personal",
  DeclaracionHijosPersonalNegativa: "Respuesta sobre hijo/a del personal",
}[tipo] ?? tipo);

const condicionesPrioridad = {
  articulacion: {
    afirmativa: "ArticulacionPedagogica",
    negativa: "DeclaracionArticulacionNegativa",
    consulta: "ConsultaArticulacionPedagogica",
  },
  hermanos: {
    afirmativa: "HermanosYGruposConvivientes",
    negativa: "DeclaracionHermanosNegativa",
  },
  personal: {
    afirmativa: "HijosDelPersonal",
    negativa: "DeclaracionHijosPersonalNegativa",
  },
};

function buscarCondicion(condiciones, tipos) {
  return [...condiciones]
    .filter((condicion) => tipos.includes(condicion.tipo))
    .sort((a, b) => new Date(b.fechaRegistro) - new Date(a.fechaRegistro))[0];
}

function estadoPrioridad(condicion, tipos) {
  if (!condicion) {
    return { respuesta: "Sin declaración registrada", estado: "NoDeclarada" };
  }
  if (condicion.tipo === tipos.afirmativa) {
    return { respuesta: "Sí", estado: condicion.estadoVerificacion };
  }
  if (condicion.tipo === tipos.consulta) {
    return { respuesta: "No está seguro/a", estado: condicion.estadoVerificacion };
  }
  return { respuesta: "No", estado: "NoDeclarada" };
}

function tarjetaPrioridad({ numero: orden, titulo, condicion, tipos, detalle, extra = "", puedeEditar = false }) {
  const estado = estadoPrioridad(condicion, tipos);
  const detalleDeclarado = condicion?.evidenciaReferencia || condicion?.valorDeclarado;
  return `<article class="prioridad-ficha">
    <div class="prioridad-ficha__cabecera"><span class="prioridad-ficha__numero">${orden}</span><div><h3>${titulo}</h3><p>Respuesta familiar: <strong>${e(estado.respuesta)}</strong></p></div>${etiqueta(estado.estado)}</div>
    <div class="prioridad-ficha__detalle">${detalle(condicion, detalleDeclarado)}${condicion?.observacion ? `<p><strong>Observación del jardín:</strong> ${e(condicion.observacion)}</p>` : ""}${extra}${puedeEditar && condicion ? `<div class="prioridad-ficha__acciones"><button class="boton boton--secundario boton--pequeno" data-accion="corregir-prioridad" data-id="${e(condicion.id)}">Editar respuesta</button></div>` : ""}</div>
  </article>`;
}

export async function ficha(ruta, ctx) {
  const [, id, pestana = "solicitud"] = ruta.split("/");
  if (!id) throw new Error("Elegí una solicitud desde el listado.");
  const rutaSolicitud = `${base(ctx.estado)}/solicitudes/${encodeURIComponent(id)}`;
  const s = ctx.estado.previa
    ? detalleEjemplo(id)
    : await ctx.api(rutaSolicitud);
  const persona = ctx.estado.previa
    ? s
    : await ctx.api(
        `${base(ctx.estado)}/aspirantes/${encodeURIComponent(s.aspiranteId)}`,
      );
  const asignacionesSeccion = ctx.estado.previa || pestana !== "solicitud"
    ? []
    : await ctx.api(
        `/periodos/${encodeURIComponent(s.periodoId)}/secciones/asignaciones-activas`,
      );
  const asignacionSeccion = asignacionesSeccion.find(
    (asignacion) => asignacion.solicitudId === s.id,
  );
  const periodoFicha = ctx.estado.periodos.find((p) => p.id === s.periodoId);
  const recepcionAbierta =
    ctx.estado.previa || periodoFicha?.estado === "RecepcionAbierta";
  const puedeCorregirDatos =
    recepcionAbierta &&
    (ctx.estado.previa ||
      ctx.estado.usuario?.permisos?.includes(
        "Solicitudes.CorregirDuranteRecepcion",
      ));
  const cabecera = encabezado({
    titulo: `${persona.nombre} ${persona.apellido}`,
    descripcion: `Solicitud ${s.id} · Ciclo ${ctx.estado.periodos.find((p) => p.id === s.periodoId)?.ciclo ?? ""}`,
    accion: volver,
  });
  const pestanas = `<nav class="pestanas" aria-label="Secciones de la ficha">${[
    ["solicitud", "Datos de inscripción"],
    ["prioridades", "Prioridades"],
    ["documentos", "Documentación y condiciones"],
    ["domicilio", "Domicilio y distancia"],
    ["responsables", "Responsables"],
  ]
    .map(
      ([clave, nombre]) =>
        `<a class="pestanas__enlace ${pestana === clave ? "pestanas__enlace--activo" : ""}" href="#solicitud/${encodeURIComponent(id)}/${clave}" ${pestana === clave ? 'aria-current="page"' : ""}>${nombre}</a>`,
    )
    .join("")}</nav>`;
  ctx.ficha = { solicitud: s, persona, rutaSolicitud };
  let contenido;
  if (pestana === "prioridades") {
    const [antecedentes, revision, distancias] = ctx.estado.previa
      ? [{ condiciones: [] }, { estado: "PendienteRevision", historial: [] }, []]
      : await Promise.all([
          ctx.api(`${rutaSolicitud}/antecedentes`),
          ctx.api(`${rutaSolicitud}/revision-domicilio`),
          ctx.api(`${rutaSolicitud}/distancias`),
        ]);
    const condiciones = antecedentes.condiciones ?? [];
    ctx.ficha.antecedentes = antecedentes;
    const articulacion = buscarCondicion(condiciones, [
      condicionesPrioridad.articulacion.afirmativa,
      condicionesPrioridad.articulacion.negativa,
      condicionesPrioridad.articulacion.consulta,
    ]);
    const hermanos = buscarCondicion(condiciones, [
      condicionesPrioridad.hermanos.afirmativa,
      condicionesPrioridad.hermanos.negativa,
    ]);
    const personal = buscarCondicion(condiciones, [
      condicionesPrioridad.personal.afirmativa,
      condicionesPrioridad.personal.negativa,
    ]);
    const distanciaActual = distancias.find((medicion) => medicion.esVigente);
    const tarjetaCercania = `<article class="prioridad-ficha">
      <div class="prioridad-ficha__cabecera"><span class="prioridad-ficha__numero">3</span><div><h3>Cercanía al jardín</h3><p>Se evalúa con el domicilio y una distancia vigente registrada.</p></div>${etiqueta(distanciaActual ? "ManualProvisional" : "PendienteRevision")}</div>
      <div class="prioridad-ficha__detalle"><p><strong>Domicilio declarado:</strong> ${e(direccionTexto(s.domicilio))}</p>${distanciaActual ? `<p><strong>Distancia registrada:</strong> ${numero(distanciaActual.distanciaMetros)} m</p><p><strong>Referencia de la medición:</strong> ${e(distanciaActual.referencia || "Sin referencia informada")}</p>` : `<p>${distancias.length ? "La última medición quedó desactualizada; hace falta registrar una nueva." : "Todavía no hay una distancia registrada para esta solicitud."}</p>`}<p><strong>Revisión del domicilio:</strong> ${e(revision.estado ?? "Sin revisión")}</p><p class="formulario__nota">La medición manual es provisoria. La distancia aporta información para evaluar esta prioridad; no confirma por sí sola una vacante.</p><a class="boton boton--texto boton--pequeno" href="#solicitud/${encodeURIComponent(id)}/domicilio">Gestionar domicilio y distancia</a></div>
    </article>`;
    ctx.ficha.prioridades = { condiciones, revision, distancias };
    contenido = tarjeta({
      titulo: "Prioridades declaradas y verificadas",
      descripcion:
        `${!recepcionAbierta ? "La recepción está cerrada: las respuestas de la familia ya no se pueden modificar. " : puedeCorregirDatos ? "Mientras la recepción esté abierta, podés corregir las declaraciones; el cambio las deja pendientes de revisión. " : "La recepción está abierta, pero tu cuenta no tiene permiso para corregir estas declaraciones. "}Consultá lo informado por la familia y lo verificado por el jardín. Una respuesta afirmativa no concede prioridad hasta que corresponda y quede verificada.`,
      cuerpo: `<div class="prioridades-ficha">${tarjetaPrioridad({
        numero: 1,
        titulo: "Articulación pedagógica",
        condicion: articulacion,
        tipos: condicionesPrioridad.articulacion,
        puedeEditar: puedeCorregirDatos,
        detalle: (condicion, dato) => {
          if (!condicion) return '<p>No se registró una respuesta sobre articulación.</p>';
          if (condicion.tipo === condicionesPrioridad.articulacion.negativa) return `<p>${e(condicion.valorDeclarado)}</p>`;
          const prefijo = "Institución de procedencia informada por la familia: ";
          const institucion = condicion.evidenciaReferencia?.startsWith(prefijo)
            ? condicion.evidenciaReferencia.slice(prefijo.length)
            : condicion.evidenciaReferencia;
          return institucion ? `<p><strong>Institución de procedencia informada:</strong> ${e(institucion)}</p>` : `<p>${e(dato)}</p>`;
        },
      })}${tarjetaPrioridad({
        numero: 2,
        titulo: "Hermanos o grupo conviviente",
        condicion: hermanos,
        tipos: condicionesPrioridad.hermanos,
        puedeEditar: puedeCorregirDatos,
        detalle: (condicion, dato) => condicion?.tipo === condicionesPrioridad.hermanos.afirmativa
          ? `<p><strong>Dato informado por la familia:</strong> ${e(dato || "Sin detalle informado")}</p>`
          : `<p>${e(condicion?.valorDeclarado ?? "No hay respuesta registrada.")}</p>`,
      })}${tarjetaCercania}${tarjetaPrioridad({
        numero: 4,
        titulo: "Hijo/a del personal",
        condicion: personal,
        tipos: condicionesPrioridad.personal,
        puedeEditar: puedeCorregirDatos,
        detalle: (condicion, dato) => condicion?.tipo === condicionesPrioridad.personal.afirmativa
          ? `<p><strong>Dato informado por la familia:</strong> ${e(dato || "Sin detalle informado")}</p>`
          : `<p>${e(condicion?.valorDeclarado ?? "No hay respuesta registrada.")}</p>`,
      })}</div>`,
    });
  } else if (pestana === "domicilio") {
    const [revision, distancias] = ctx.estado.previa
      ? [
          {
            estado: "PendienteRevision",
            historial: [],
            revisionSolicitud: 1,
            revisionDomicilio: 1,
            ultimoNumeroRegistro: 0,
          },
          [],
        ]
      : await Promise.all([
          ctx.api(`${rutaSolicitud}/revision-domicilio`),
          ctx.api(`${rutaSolicitud}/distancias`),
        ]);
    ctx.ficha.revision = revision;
    ctx.ficha.distancias = distancias;
    const actual = distancias.find((m) => m.esVigente);
    contenido = `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Domicilio declarado", cuerpo: `<p>${e(direccionTexto(s.domicilio))}</p>${etiqueta(revision.estado)}<hr class="separador"><p class="formulario__nota">La revisión manual corresponde a esta versión de la dirección.</p><div class="formulario__acciones"><button class="boton boton--secundario" data-accion="revisar-domicilio">Registrar revisión</button></div>` })}${tarjeta({ titulo: "Distancia al jardín", cuerpo: actual ? `<div class="indicador__numero">${numero(actual.distanciaMetros)} <small>m</small></div><p>${etiqueta("ManualProvisional")}</p><p class="formulario__nota">${e(actual.referencia)}</p><div class="formulario__acciones"><button class="boton boton--secundario" data-accion="distancia-manual">Registrar otra distancia</button></div>` : `<p class="tarjeta__descripcion">${distancias.length ? "Las mediciones anteriores están desactualizadas." : "Todavía no hay una distancia registrada."}</p><div class="formulario__acciones"><button class="boton boton--secundario" data-accion="distancia-manual">Cargar distancia manual</button></div>` })}</div><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Historial de revisión de domicilio</h2></div>${revision.historial.length ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Fecha</th><th>Estado</th><th>Motivo</th><th>Referencia</th><th>Vigencia</th></tr></thead><tbody>${revision.historial.map((r) => `<tr><td>${fecha(r.fecha)}</td><td>${etiqueta(r.estado)}</td><td>${e(r.motivo)}</td><td>${e(r.referencia ?? "—")}</td><td>${r.esActual ? "Actual" : "Histórica"}</td></tr>`).join("")}</tbody></table></div>` : '<div class="vacio"><h2>Sin revisiones registradas</h2><p>Podés dejar constancia de una observación o comprobación manual.</p></div>'}</section><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Historial de distancias</h2></div>${distancias.length ? `<div class="tabla__contenedor"><table class="tabla"><thead><tr><th>Fecha</th><th>Distancia</th><th>Origen</th><th>Referencia</th><th>Vigencia</th></tr></thead><tbody>${distancias.map((m) => `<tr><td>${fecha(m.fechaRegistro)}</td><td>${numero(m.distanciaMetros)} m</td><td>${etiqueta(m.origen)}</td><td>${e(m.referencia)}</td><td>${m.esVigente ? "Actual" : "Histórica / desactualizada"}</td></tr>`).join("")}</tbody></table></div>` : '<div class="tarjeta__cuerpo"><p class="formulario__nota">Una distancia pendiente no se interpreta como cero.</p></div>'}</section>`;
  } else if (pestana === "documentos") {
    const a = ctx.estado.previa
      ? { revisionSolicitud: 1, documentos: [], condiciones: [] }
      : await ctx.api(`${rutaSolicitud}/antecedentes`);
    ctx.ficha.antecedentes = a;
    contenido = `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Documentación", accion: '<button class="boton boton--texto boton--pequeno" data-accion="nuevo-documento">+ Registrar</button>', cuerpo: a.documentos.length ? a.documentos.map((d) => `<div class="acceso-rapido"><div class="acceso-rapido__texto"><strong class="acceso-rapido__titulo">${e(d.tipo)}</strong><span class="acceso-rapido__descripcion">${e(d.referencia ?? "Sin referencia")}</span></div>${etiqueta(d.estado)}<button class="boton boton--texto boton--pequeno" data-accion="revisar-documento" data-id="${e(d.id)}">Revisar</button></div>`).join("") : '<p class="formulario__nota">No hay documentos registrados. Esto no significa que el legajo esté completo.</p>' })}${tarjeta({ titulo: "Condiciones declaradas", accion: '<button class="boton boton--texto boton--pequeno" data-accion="nueva-condicion">+ Declarar</button>', cuerpo: a.condiciones.length ? a.condiciones.map((c) => `<div class="acceso-rapido"><div class="acceso-rapido__texto"><strong class="acceso-rapido__titulo">${e(nombreCondicion(c.tipo))}</strong><span class="acceso-rapido__descripcion">${e(c.valorDeclarado)}</span></div>${c.tipo.endsWith("Negativa") ? etiqueta("NoDeclarada") : etiqueta(c.estadoVerificacion)}<button class="boton boton--texto boton--pequeno" data-accion="revisar-condicion" data-id="${e(c.id)}">Revisar</button></div>`).join("") : '<p class="formulario__nota">Sin condiciones declaradas. Declarar una condición no la verifica ni concede prioridad automáticamente.</p>' })}</div>`;
  } else if (pestana === "responsables") {
    const vinculos = persona.responsables ?? [];
    contenido = tarjeta({
      titulo: "Responsables del aspirante",
      accion:
        '<button class="boton boton--secundario boton--pequeno" data-accion="responsable">Agregar responsable</button>',
      cuerpo: vinculos.length
        ? vinculos
            .map(
              (v) =>
                `<div class="acceso-rapido"><div><strong>${e(v.responsable.nombre)} ${e(v.responsable.apellido)}</strong><p class="tarjeta__descripcion">${e(v.tipoVinculo)} · ${e(v.responsable.telefono)}</p></div></div>`,
            )
            .join("")
        : '<p class="formulario__nota">No hay responsables vinculados. Podés registrar uno nuevo o vincular uno existente.</p>',
    });
  } else {
    const ayudaEdicion = recepcionAbierta
      ? "Mientras la recepción esté abierta, podés corregir estos datos. Si cambiás la fecha de nacimiento, la edad y la sala de las solicitudes de este aspirante en el período se recalculan con la fecha de corte indicada."
      : "La recepción está cerrada. Los datos del aspirante y de la inscripción ya no se pueden corregir.";
    const asignacionActual = asignacionSeccion
      ? `<div class="campo--completo"><dt>Asignación actual</dt><dd>Sala ${e(asignacionSeccion.sala)} · ${e(ctx.estado.turnos.find((t) => t.id === asignacionSeccion.turnoId)?.nombre ?? "Turno registrado")} · Sección ${e(asignacionSeccion.nombreSeccion)}</dd></div>`
      : "";
    contenido = `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Datos del aspirante", accion: puedeCorregirDatos ? '<button class="boton boton--secundario boton--pequeno" data-accion="editar-aspirante">Editar aspirante</button>' : '<button class="boton boton--secundario boton--pequeno" disabled aria-label="Editar aspirante: disponible cuando la recepción esté abierta">Editar aspirante</button>', cuerpo: `<dl class="datos"><div><dt>Nombre</dt><dd>${e(persona.nombre)}</dd></div><div><dt>Apellido</dt><dd>${e(persona.apellido)}</dd></div><div><dt>Fecha de nacimiento</dt><dd>${fecha(persona.fechaNacimiento ?? s.fechaNacimientoUtilizada)}</dd></div><div><dt>DNI</dt><dd>${e(persona.dni || "Sin DNI declarado")}</dd></div><div><dt>Tipo de documento</dt><dd>${e(persona.tipoDocumento || "No informado")}</dd></div><div><dt>Sexo</dt><dd>${e(persona.sexo || "No informado")}</dd></div><div><dt>Grupo etario</dt><dd>${s.sala ? `Sala de ${e(s.sala)} años` : "Pendiente de revisión de edad"}</dd></div><div><dt>Fecha de corte utilizada</dt><dd>${fecha(s.fechaCorteUtilizada)}</dd></div></dl><p class="formulario__nota">${ayudaEdicion}</p>` })}${tarjeta({ titulo: "Datos de inscripción", cuerpo: `<dl class="datos"><div><dt>Recepción</dt><dd>${etiqueta(s.estadoRecepcion)}</dd></div><div><dt>Turno preferido</dt><dd>${e(ctx.estado.turnos.find((t) => t.id === s.turnoPreferidoId)?.nombre ?? "Turno registrado")}</dd></div>${asignacionActual}<div class="campo--completo"><dt>Domicilio</dt><dd>${e(direccionTexto(s.domicilio))}</dd></div><div><dt>Fecha de registro</dt><dd>${fecha(s.fechaRegistro)}</dd></div><div><dt>Referencia de consentimiento</dt><dd>${e(s.referenciaConsentimiento ?? "No informada")}</dd></div></dl>${puedeCorregirDatos ? '<div class="formulario__acciones"><button class="boton boton--secundario" data-accion="corregir-solicitud">Corregir datos de solicitud</button></div>' : ""}` })}</div>`;
  }
  return cabecera + pestanas + contenido;
}
