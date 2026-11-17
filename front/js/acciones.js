import { atenderAccionOrganizacion, atenderFormularioOrganizacion } from "./acciones-organizacion.js?v=20261117-lista-espera-filtros-v2";
import { atenderAdmision } from "./acciones-admision.js?v=20261117-aceptaciones-lote-v2";
import {
  escapar as e,
  campo,
  seleccion,
  aviso,
  errorFormulario,
} from "./interfaz.js";
import { base } from "./compartidos.js";
import { camposDomicilio, opcionesPrioridad } from "./pagina-solicitudes.js";
import { actualizarBorrador, claveBorradores } from "./borradores-solicitud.js";
import { motivo, motivos } from "./operaciones-formularios.js";
import { edadesDeReglas, fechaNacimientoAdmitida, rangoFechasNacimiento } from "./reglas-edad.js";
const opciones = (nombres) => nombres.map(([id, nombre]) => ({ id, nombre }));
const accionesFormulario = (previa, textoConfirmacion = "Guardar") =>
  `${errorFormulario()}${previa ? '<p class="formulario__nota">Vista previa: los cambios no se guardan.</p>' : ""}<div class="formulario__acciones"><button class="boton boton--secundario" type="button" data-accion="cerrar-dialogo">Cancelar</button><button class="boton" type="submit" ${previa ? "disabled" : ""}>${e(textoConfirmacion)}</button></div>`;
function mostrar(ctx, datos) {
  ctx.operacion = {
    institucion: ctx.estado.institucion,
    periodo: ctx.estado.periodo,
    ficha: ctx.ficha,
    id: datos.id,
    versiones: null,
    responsableCreado: null,
  };
  ctx.abrirDialogo({
    titulo: datos.titulo,
    descripcion: datos.descripcion ?? "",
    contenido: `<form class="formulario" data-formulario="${datos.formulario}">${datos.contenido}${accionesFormulario(ctx.estado.previa, datos.textoConfirmacion)}</form>`,
  });
}
function camposOfertaDeCiclo(oferta = {}, capacidadReferencia = "") {
  return `<div class="formulario__rejilla">${campo({ nombre: "capacidad", etiqueta: "Capacidad para este ciclo", tipo: "number", valor: oferta.capacidad ?? capacidadReferencia, obligatorio: true, ayuda: "Este valor solo afecta al ciclo seleccionado.", atributos: 'min="0" step="1"' })}</div><label class="casilla"><input type="checkbox" name="disponible" ${oferta.disponible === false ? "" : "checked"}>Habilitada para este ciclo</label>${motivo(motivos.oferta, { etiqueta: "Motivo del cambio" })}`;
}

const confirmacionOfertaPublicada = (periodo) =>
  periodo?.estado === "RecepcionAbierta" && periodo.versionReglasId
    ? '<label class="casilla"><input type="checkbox" name="confirmarPublicacion" required>La recepción está abierta. Confirmo que este cambio se publique para las nuevas solicitudes.</label>'
    : "";

async function publicarOfertaDelCiclo(ctx, operacion, motivoCambio) {
  const periodo = operacion.periodo;
  if (!periodo?.versionReglasId) return false;
  const ruta = `/periodos/${encodeURIComponent(periodo.id)}`;
  operacion.versiones ??= {};
  if (!operacion.versiones.oferta)
    operacion.versiones.oferta = (
      await ctx.api(`${ruta}/versiones-oferta`, {
        metodo: "POST",
        datos: {
          referencia: `Actualización automática de oferta del ciclo ${periodo.ciclo}`,
          motivo: motivoCambio,
        },
      })
    ).id;
  await ctx.api(`${ruta}/configuracion`, {
    metodo: "PUT",
    datos: {
      versiones: {
        versionReglasId: periodo.versionReglasId,
        versionOfertaId: operacion.versiones.oferta,
      },
      revision: periodo.revision,
      motivo: motivoCambio,
    },
  });
  return true;
}

export async function atenderAccion(elemento, ctx) {
  if (elemento.dataset.accion === "org-aceptaciones-turno-lote")
    return atenderAdmision(elemento, ctx);
  if (await atenderAccionOrganizacion(elemento, ctx)) return;
  const accion = elemento.dataset.accion;
  const { estado } = ctx;
  if (accion === "pagina-anterior" || accion === "pagina-siguiente") {
    estado.pagina += accion === "pagina-anterior" ? -1 : 1;
    await ctx.actualizar();
    return;
  }
  if (accion === "limpiar-filtros") {
    estado.filtros = {};
    estado.pagina = 1;
    await ctx.actualizar();
    return;
  }
  if (accion === "nueva-institucion" || accion === "editar-institucion") {
    const d = accion === "editar-institucion" ? estado.institucion : {};
    mostrar(ctx, {
      titulo: d.id ? "Editar datos del jardín" : "Registrar institución",
      formulario: d.id ? "editar-institucion" : "institucion",
      contenido: `<div class="formulario__rejilla">${campo({ nombre: "nombre", etiqueta: "Nombre del jardín", valor: d.nombre, obligatorio: true, completo: true })}${campo({ nombre: "codigo", etiqueta: "Código institucional (opcional)", valor: d.codigo })}${campo({ nombre: "telefonoContacto", etiqueta: "Teléfono", valor: d.telefonoContacto, tipo: "tel", obligatorio: true })}${campo({ nombre: "correoElectronicoContacto", etiqueta: "Correo (opcional)", valor: d.correoElectronicoContacto, tipo: "email", completo: true })}${camposDomicilio(d.domicilio)}${campo({ nombre: "latitud", etiqueta: "Latitud (opcional)", tipo: "number", valor: d.coordenadas?.latitud, atributos: 'step="any" min="-90" max="90"' })}${campo({ nombre: "longitud", etiqueta: "Longitud (opcional)", tipo: "number", valor: d.coordenadas?.longitud, atributos: 'step="any" min="-180" max="180"' })}${d.id ? motivo(motivos.cambioInstitucion, { etiqueta: "Motivo del cambio" }) : ""}</div>`,
    });
    return;
  }
  if (accion === "nuevo-periodo") {
    mostrar(ctx, {
      titulo: "Crear período de inscripción",
      formulario: "periodo",
      contenido:
        campo({
          nombre: "ciclo",
          etiqueta: "Ciclo lectivo",
          tipo: "number",
          obligatorio: true,
          ayuda: "La institución puede tener un solo período por ciclo lectivo.",
          atributos: 'min="1" max="9999" step="1" placeholder="2027"',
        }) +
        `<div class="formulario__rejilla">${campo({ nombre: "inicio", etiqueta: "Inicio previsto", tipo: "date", obligatorio: true })}${campo({ nombre: "fin", etiqueta: "Fin previsto", tipo: "date", obligatorio: true })}</div><p class="formulario__nota">El período se crea en preparación. La recepción se abre después.</p>`,
    });
    return;
  }
  if (accion === "recepcion") {
    const p = estado.periodos.find((p) => p.id === elemento.dataset.id);
    mostrar(ctx, {
      titulo:
        p.estado === "Preparado"
          ? "Abrir recepción"
          : "Cerrar recepción ordinaria",
      formulario: "recepcion",
      id: p.id,
      contenido:
        aviso(
          p.estado === "Preparado"
            ? "La apertura es manual. Prepará y asigná reglas y oferta antes de registrar solicitudes."
            : "Las nuevas solicitudes quedarán fuera de término, pendientes de autorización. Las revisiones podrán continuar.",
          "aviso",
        ) + motivo(p.estado === "Preparado" ? motivos.aperturaRecepcion : motivos.cierreRecepcion),
    });
    ctx.operacion.periodo = p;
    return;
  }
  if (accion === "nueva-aula" || accion === "nuevo-turno") {
    mostrar(ctx, {
      titulo: accion === "nueva-aula" ? "Registrar aula" : "Registrar turno",
      formulario: accion,
      contenido: campo({
        nombre: "nombre",
        etiqueta: "Nombre",
        obligatorio: true,
      }),
    });
    return;
  }
  if (accion === "nueva-oferta-institucional") {
    if (!estado.aulas.length || !estado.turnos.length)
      throw new Error("Primero registrá al menos un aula y un turno.");
    mostrar(ctx, {
      titulo: "Crear oferta reutilizable",
      descripcion: "Guardá una combinación base de aula, turno y capacidad para agregarla a los ciclos que la necesiten.",
      formulario: "oferta-institucional",
      contenido: `<div class="formulario__rejilla">${seleccion({ nombre: "aulaId", etiqueta: "Aula", opciones: estado.aulas, obligatorio: true })}${seleccion({ nombre: "turnoId", etiqueta: "Turno", opciones: estado.turnos, obligatorio: true })}${campo({ nombre: "capacidad", etiqueta: "Capacidad de referencia", tipo: "number", obligatorio: true, ayuda: "Es el valor sugerido al copiar esta oferta a un ciclo.", atributos: 'min="0" step="1"' })}${campo({ nombre: "referenciaCapacidad", etiqueta: "Referencia de capacidad (opcional)", ayuda: "Podés indicar un acta, planilla o norma que respalde la capacidad informada." })}</div>${motivo(motivos.ofertaInstitucional, { etiqueta: "Motivo del registro" })}`,
    });
    return;
  }
  if (accion === "editar-oferta-institucional") {
    const oferta = estado.ofertasInstitucionales.find((item) => item.id === elemento.dataset.id);
    if (!oferta) throw new Error("No se encontró la oferta reutilizable.");
    mostrar(ctx, {
      titulo: "Editar oferta reutilizable",
      descripcion: "Los cambios se usarán como referencia al agregar esta combinación a otros ciclos. Las ofertas ya agregadas conservan sus propios datos.",
      formulario: "editar-oferta-institucional",
      contenido: `<div class="formulario__rejilla">${seleccion({ nombre: "aulaId", etiqueta: "Aula", opciones: estado.aulas, valor: oferta.aulaId, obligatorio: true })}${seleccion({ nombre: "turnoId", etiqueta: "Turno", opciones: estado.turnos, valor: oferta.turnoId, obligatorio: true })}${campo({ nombre: "capacidad", etiqueta: "Capacidad de referencia", tipo: "number", valor: oferta.capacidad, obligatorio: true, atributos: 'min="0" step="1"' })}${campo({ nombre: "referenciaCapacidad", etiqueta: "Referencia (opcional)", valor: oferta.referenciaCapacidad === "Sin referencia informada" ? "" : oferta.referenciaCapacidad })}</div>${motivo(motivos.ofertaInstitucional, { etiqueta: "Motivo del cambio" })}`,
    });
    ctx.operacion.ofertaInstitucional = oferta;
    return;
  }
  if (accion === "agregar-oferta-periodo") {
    const plantilla = estado.ofertasInstitucionales.find((item) => item.id === elemento.dataset.id);
    if (!plantilla || !estado.periodo)
      throw new Error("Seleccioná una oferta reutilizable y un período para continuar.");
    const aula = estado.aulas.find((item) => item.id === plantilla.aulaId)?.nombre ?? "Aula";
    const turno = estado.turnos.find((item) => item.id === plantilla.turnoId)?.nombre ?? "Turno";
    if (estado.ofertas.some((item) => item.aulaId === plantilla.aulaId && item.turnoId === plantilla.turnoId))
      throw new Error("Esta combinación ya está agregada al período seleccionado.");
    mostrar(ctx, {
      titulo: `Agregar oferta al ciclo ${estado.periodo.ciclo}`,
      descripcion: `${aula} · ${turno}. Se copiará desde las ofertas reutilizables y podrás ajustar sus datos para este ciclo.`,
      textoConfirmacion: estado.periodo.versionReglasId ? "Guardar y aplicar" : "Guardar oferta",
      formulario: "agregar-oferta-periodo",
      contenido: camposOfertaDeCiclo({}, plantilla.capacidad) + '<p class="formulario__nota">La oferta reutilizable no se modifica; esta copia tendrá disponibilidad y capacidad propias del ciclo.</p>' + confirmacionOfertaPublicada(estado.periodo),
    });
    ctx.operacion.ofertaPlantilla = plantilla;
    return;
  }
  if (accion === "editar-oferta-periodo") {
    const oferta = estado.ofertas.find((item) => item.id === elemento.dataset.id);
    if (!oferta || !estado.periodo)
      throw new Error("No se encontró la oferta del período seleccionado.");
    const aula = estado.aulas.find((item) => item.id === oferta.aulaId)?.nombre ?? "Aula";
    const turno = estado.turnos.find((item) => item.id === oferta.turnoId)?.nombre ?? "Turno";
    mostrar(ctx, {
      titulo: `Modificar oferta del ciclo ${estado.periodo.ciclo}`,
      descripcion: `${aula} · ${turno}. Los cambios se aplicarán solo a este período.`,
      textoConfirmacion: estado.periodo.versionReglasId ? "Guardar y aplicar" : "Guardar cambios",
      formulario: "editar-oferta-periodo",
      contenido: camposOfertaDeCiclo(oferta) + (estado.periodo.versionReglasId ? '<p class="formulario__nota">Al guardar, la nueva versión de la oferta se aplicará automáticamente a este ciclo.</p>' : '<p class="formulario__nota">El ciclo todavía no tiene reglas publicadas; el cambio quedará en preparación hasta configurar el ciclo.</p>') + confirmacionOfertaPublicada(estado.periodo),
    });
    ctx.operacion.ofertaPeriodo = oferta;
    return;
  }
  if (accion === "preparar-ciclo") {
    const faltaReglas = !estado.periodo?.versionReglasId;
    if (!estado.periodo || !estado.ofertas.length)
      throw new Error("Agregá primero al menos una oferta al período para preparar el ciclo.");
    mostrar(ctx, {
      titulo: `Preparar ciclo ${estado.periodo.ciclo}`,
      descripcion: "La preparación inicial vincula a este ciclo sus reglas y la oferta cargada.",
      textoConfirmacion: "Preparar ciclo",
      formulario: "preparar-ciclo",
      contenido:
        aviso(
          "La fecha de corte queda asociada a este período. La oferta se publica como una instantánea independiente. Las versiones anteriores se conservan.",
          "aviso",
        ) +
        (faltaReglas ? campo({
          nombre: "fechaCorte",
          etiqueta: "Fecha de corte para calcular edades",
          tipo: "date",
          obligatorio: true,
          valor: estado.fechaCortePeriodo ?? "",
          ayuda:
            "Es la fecha que el sistema usa para calcular la edad y el grupo etario de cada aspirante en este ciclo.",
        }) : `<p class="formulario__nota">Se conservará la fecha de corte ya configurada para este ciclo: ${e(estado.fechaCortePeriodo)}.</p>`) +
        motivo(motivos.publicacion, { etiqueta: "Motivo de la publicación" }),
    });
    return;
  }
  if (accion === "editar-fecha-corte") {
    mostrar(ctx, {
      titulo: `Cambiar fecha de corte del ciclo ${estado.periodo.ciclo}`,
      descripcion: "Se actualizarán las reglas de este ciclo. La oferta publicada y sus capacidades no se modificarán.",
      textoConfirmacion: "Guardar fecha de corte",
      formulario: "editar-fecha-corte",
      contenido: campo({
        nombre: "fechaCorte",
        etiqueta: "Fecha de corte para calcular edades",
        tipo: "date",
        obligatorio: true,
        valor: estado.fechaCortePeriodo ?? "",
        ayuda: "La usarán las nuevas solicitudes de este ciclo. Las solicitudes anteriores conservan la fecha con la que fueron registradas.",
      }) + motivo(motivos.publicacion, { etiqueta: "Motivo del cambio" }),
    });
    return;
  }
  if (accion === "publicar-oferta-periodo") {
    if (!estado.periodo?.versionReglasId)
      throw new Error("Configurá primero las reglas y la fecha de corte del ciclo.");
    if (!estado.ofertas.length)
      throw new Error("Agregá al menos una oferta al período para poder publicarla.");
    mostrar(ctx, {
      titulo: `Aplicar oferta del ciclo ${estado.periodo.ciclo}`,
      descripcion: "Se actualizará la oferta disponible para nuevas solicitudes. Las reglas y la fecha de corte se conservan.",
      textoConfirmacion: "Aplicar cambios",
      formulario: "publicar-oferta-periodo",
      contenido: aviso("Este paso sirve para recuperar cambios que no llegaron a aplicarse automáticamente. Solo afecta a este ciclo y conserva la fecha de corte vigente.", "aviso") + motivo(motivos.oferta, { etiqueta: "Motivo de la aplicación" }) + confirmacionOfertaPublicada(estado.periodo),
    });
    return;
  }
  if (accion === "detectar-duplicados") {
    if (estado.previa) {
      ctx.notificar("Vista previa: la detección no se ejecuta.");
      return;
    }
    elemento.disabled = true;
    try {
      const r = await ctx.api(
        `${base(estado)}/periodos/${encodeURIComponent(estado.periodo.id)}/duplicados/detectar`,
        { metodo: "POST" },
      );
      ctx.notificar(
        `Detección completada: ${r.nuevas} alertas nuevas y ${r.actualizadas} actualizadas.`,
      );
      await ctx.actualizar();
    } finally {
      elemento.disabled = false;
    }
    return;
  }
  if (accion === "resolver-duplicado") {
    const d = ctx.duplicados.find((d) => d.id === elemento.dataset.id);
    mostrar(ctx, {
      titulo: "Registrar decisión sobre duplicación",
      formulario: "duplicado",
      id: d.id,
      contenido:
        aviso(
          "Ambas solicitudes se conservan. La decisión no las fusiona ni excluye automáticamente.",
        ) +
        seleccion({
          nombre: "decision",
          etiqueta: "Decisión",
          opciones: opciones([
            ["DuplicacionConfirmada", "Duplicación confirmada"],
            ["PersonasDiferentes", "Personas diferentes"],
          ]),
        }) +
        motivo(motivos.duplicado, { etiqueta: "Motivo de la decisión" }),
    });
    ctx.operacion.alerta = d;
    return;
  }
  const ficha = ctx.ficha;
  if (!ficha) return;
  if (accion === "distancia-manual") {
    mostrar(ctx, {
      titulo: "Registrar distancia manual",
      formulario: "distancia",
      contenido:
        aviso(
          "Ingresá una distancia directa obtenida manualmente. Se conservará como provisoria, con su referencia.",
          "aviso",
        ) +
        campo({
          nombre: "distanciaMetros",
          etiqueta: "Distancia en metros",
          tipo: "number",
          obligatorio: true,
          atributos: 'min="0" step="any"',
        }) +
        campo({
          nombre: "referencia",
          etiqueta: "Cómo se obtuvo la distancia",
          obligatorio: true,
        }),
    });
    return;
  }
  if (accion === "revisar-domicilio") {
    mostrar(ctx, {
      titulo: "Revisar domicilio",
      formulario: "revision-domicilio",
      contenido:
        seleccion({
          nombre: "estado",
          etiqueta: "Estado de revisión",
          opciones: opciones([
            ["PendienteRevision", "Pendiente de revisión"],
            ["Observado", "Observado"],
            ["RevisadoManualmente", "Revisado manualmente"],
          ]),
        }) +
        motivo(motivos.revisionDomicilio) +
        campo({
          nombre: "referencia",
          etiqueta: "Referencia de comprobación",
          ayuda: "Obligatoria si se marca Revisado manualmente.",
        }),
    });
    return;
  }
  if (accion === "corregir-solicitud") {
    const s = ficha.solicitud;
    mostrar(ctx, {
      titulo: "Corregir datos de solicitud",
      descripcion:
        "La corrección conserva los datos anteriores en el historial.",
      formulario: "corregir",
      contenido: `<div class="formulario__rejilla">${camposDomicilio(s.domicilio)}${seleccion({ nombre: "turnoPreferidoId", etiqueta: "Turno preferido", opciones: estado.turnos, valor: s.turnoPreferidoId, obligatorio: true })}${campo({ nombre: "referenciaConsentimiento", etiqueta: "Referencia de consentimiento", valor: s.referenciaConsentimiento })}</div>${motivo(motivos.correccionSolicitud, { etiqueta: "Motivo de la corrección" })}<p class="formulario__nota">Se conservarán las alternativas de turno ya aceptadas. Los datos personales compartidos del aspirante se corrigen mediante un procedimiento posterior.</p>`,
    });
    return;
  }
  if (accion === "editar-aspirante") {
    const aspirante = ctx.ficha.persona;
    let fechaCorteSolicitud = ctx.ficha.solicitud.fechaCorteUtilizada ?? estado.fechaCortePeriodo;
    let edadesSolicitud = estado.edadesAdmitidasPeriodo;
    if (!estado.previa && ctx.ficha.solicitud.versionReglasId) {
      const reglasHistoricas = await ctx.api(`/versiones-configuracion/${encodeURIComponent(ctx.ficha.solicitud.versionReglasId)}`);
      fechaCorteSolicitud = reglasHistoricas.datos?.FechaCorte ?? reglasHistoricas.datos?.fechaCorte ?? fechaCorteSolicitud;
      edadesSolicitud = edadesDeReglas(reglasHistoricas.datos);
    }
    const rangoNacimiento = rangoFechasNacimiento(fechaCorteSolicitud, edadesSolicitud);
    const atributosNacimiento = rangoNacimiento ? `min="${rangoNacimiento.minimo}" max="${rangoNacimiento.maximo}"` : "";
    mostrar(ctx, {
      titulo: "Editar datos del aspirante",
      descripcion: "La corrección quedará registrada en el historial.",
      formulario: "editar-aspirante",
      contenido: `<div class="formulario__rejilla">${campo({ nombre: "nombre", etiqueta: "Nombre", valor: aspirante.nombre, obligatorio: true })}${campo({ nombre: "apellido", etiqueta: "Apellido", valor: aspirante.apellido, obligatorio: true })}${campo({ nombre: "fechaNacimiento", etiqueta: "Fecha de nacimiento", tipo: "date", valor: aspirante.fechaNacimiento, obligatorio: true, atributos: atributosNacimiento })}${campo({ nombre: "dni", etiqueta: "DNI (opcional)", valor: aspirante.dni ?? "" })}${campo({ nombre: "tipoDocumento", etiqueta: "Tipo de documento (opcional)", valor: aspirante.tipoDocumento ?? "", sugerencias: ["DNI", "Pasaporte", "Otro documento"] })}${opcionesPrioridad("sexo", "Sexo", [["Masculino", "Masculino"], ["Femenino", "Femenino"]], aspirante.sexo ?? "", false)}</div><p class="formulario__nota">La fecha debe corresponder a las edades admitidas al ${e(fechaCorteSolicitud ?? "la fecha de corte del ciclo")}. Al guardar se recalculan la edad y la sala y se registra el cambio en el historial.</p>`,
    });
    ctx.operacion.edadesAdmitidas = edadesSolicitud;
    ctx.operacion.fechaCorte = fechaCorteSolicitud;
    return;
  }
  if (accion === "corregir-articulacion" || accion === "corregir-prioridad") {
    const condicion = ficha.antecedentes.condiciones.find(
      (x) => x.id === elemento.dataset.id,
    );
    if (!condicion || estado.periodo?.estado !== "RecepcionAbierta")
      throw new Error("La respuesta solo se puede cambiar mientras la recepción esté abierta.");
    const tipos = {
      articulacion: ["ArticulacionPedagogica", "DeclaracionArticulacionNegativa", "ConsultaArticulacionPedagogica"],
      hermanos: ["HermanosYGruposConvivientes", "DeclaracionHermanosNegativa"],
      personal: ["HijosDelPersonal", "DeclaracionHijosPersonalNegativa"],
    };
    const grupo = Object.entries(tipos).find(([, valores]) => valores.includes(condicion.tipo))?.[0];
    if (!grupo) throw new Error("No se reconoce el criterio de prioridad que querés corregir.");
    const respuestaActual = grupo === "articulacion"
      ? condicion.tipo === "ArticulacionPedagogica" ? "Si" : condicion.tipo === "ConsultaArticulacionPedagogica" ? "NoEstoySeguro" : "No"
      : condicion.tipo === tipos[grupo][0] ? "Si" : "No";
    const prefijoProcedencia = "Institución de procedencia informada por la familia: ";
    const valorDetalle = grupo === "articulacion"
      ? condicion.evidenciaReferencia?.startsWith(prefijoProcedencia)
        ? condicion.evidenciaReferencia.slice(prefijoProcedencia.length)
        : ""
      : condicion.tipo === tipos[grupo][0] ? condicion.evidenciaReferencia ?? "" : "";
    const nombreRespuesta = {
      articulacion: "respuestaArticulacion",
      hermanos: "respuestaHermanosGrupo",
      personal: "respuestaHijoPersonal",
    }[grupo];
    const nombreDetalle = {
      articulacion: "institucionProcedencia",
      hermanos: "detalleHermanoGrupo",
      personal: "detalleHijoPersonal",
    }[grupo];
    const etiquetaDetalle = {
      articulacion: "Institución de procedencia",
      hermanos: "Nombre del alumno/a, sala y vínculo",
      personal: "Nombre de la persona y dónde trabaja",
    }[grupo];
    const opcionesRespuesta = grupo === "articulacion"
      ? [["Si", "Sí"], ["No", "No"], ["NoEstoySeguro", "No estoy seguro/a"]]
      : [["Si", "Sí"], ["No", "No"]];
    const requiereDetalle = grupo === "articulacion"
      ? ["Si", "NoEstoySeguro"].includes(respuestaActual)
      : respuestaActual === "Si";
    const contenidoDetalle = `<div data-prioridad-detalle="${grupo}" ${requiereDetalle ? "" : "hidden"}>${campo({ nombre: nombreDetalle, etiqueta: etiquetaDetalle, valor: valorDetalle, obligatorio: requiereDetalle, completo: true })}</div>`;
    mostrar(ctx, {
      titulo: `Actualizar prioridad: ${grupo === "articulacion" ? "articulación pedagógica" : grupo === "hermanos" ? "hermanos o grupo conviviente" : "hijo/a del personal"}`,
      descripcion: "La respuesta nueva quedará pendiente de revisión y el cambio se conservará en el historial.",
      formulario: "corregir-prioridad",
      id: condicion.id,
      contenido: seleccion({
        nombre: nombreRespuesta,
        etiqueta: "Respuesta de la familia",
        valor: respuestaActual,
        opciones: opciones(opcionesRespuesta),
      }) + contenidoDetalle + motivo(motivos.condicion, { etiqueta: "Motivo del cambio" }),
    });
    return;
  }
  if (
    [
      "nuevo-documento",
      "revisar-documento",
      "nueva-condicion",
      "revisar-condicion",
    ].includes(accion)
  ) {
    const esDocumento = accion.includes("documento");
    const nuevo = accion.startsWith("nuev");
    const dato = nuevo
      ? {}
      : (esDocumento
          ? ficha.antecedentes.documentos
          : ficha.antecedentes.condiciones
        ).find((d) => d.id === elemento.dataset.id);
    mostrar(ctx, {
      titulo: `${nuevo ? "Registrar" : "Revisar"} ${esDocumento ? "documentación" : "condición"}`,
      formulario: accion,
      id: dato.id,
      contenido:
        (nuevo
          ? campo({ nombre: "tipo", etiqueta: "Tipo", obligatorio: true })
          : `<p><strong>${e(dato.tipo)}</strong></p>`) +
        (!esDocumento && nuevo
          ? campo({
              nombre: "valorDeclarado",
              etiqueta: "Valor declarado",
              obligatorio: true,
            })
          : "") +
        (esDocumento || !nuevo
          ? seleccion({
              nombre: "estado",
              etiqueta: "Estado",
              opciones: esDocumento
                ? opciones([
                    ["Pendiente", "Pendiente"],
                    ["Presentada", "Presentada"],
                    ["Observada", "Observada"],
                  ])
                : opciones([
                    ["PendienteRevision", "Pendiente de revisión"],
                    ["Verificada", "Verificada"],
                    ["Observada", "Observada"],
                    ...(estado.periodo?.estado === "RecepcionCerrada" &&
                    (dato.tipo === "ArticulacionPedagogica" || dato.tipo === "ConsultaArticulacionPedagogica")
                      ? [["NoAcreditada", "No acreditada al cierre"]]
                      : []),
                  ]),
              valor: dato.estado ?? dato.estadoVerificacion,
            })
          : '<p class="formulario__nota">La condición se registra pendiente de revisión.</p>') +
        campo({
          nombre: "referencia",
          etiqueta: "Referencia (opcional)",
          valor: dato.referencia ?? dato.evidenciaReferencia,
        }) +
        campo({
          nombre: "observacion",
          etiqueta: "Observación (opcional)",
          valor: dato.observacion,
        }) +
        motivo(esDocumento ? motivos.documentacion : motivos.condicion, {
          etiqueta: nuevo ? "Motivo del registro" : "Motivo de la revisión",
        }),
    });
    return;
  }
  if (accion === "responsable") {
    mostrar(ctx, {
      titulo: "Agregar responsable",
      formulario: "responsable",
      contenido: `<p class="formulario__nota">Si ya está registrado en esta institución, indicá su identificador. Para crear uno nuevo, completá los datos personales.</p>${campo({ nombre: "responsableId", etiqueta: "Identificador existente (opcional)" })}<div class="formulario__rejilla">${campo({ nombre: "nombre", etiqueta: "Nombre" })}${campo({ nombre: "apellido", etiqueta: "Apellido" })}${campo({ nombre: "telefono", etiqueta: "Teléfono", tipo: "tel" })}${campo({ nombre: "documento", etiqueta: "Documento (opcional)" })}${campo({ nombre: "correoElectronico", etiqueta: "Correo (opcional)", tipo: "email" })}${campo({ nombre: "tipoVinculo", etiqueta: "Relación con el aspirante", obligatorio: true })}</div><label class="casilla"><input type="checkbox" name="contactoPrincipal">Contacto principal</label><label class="casilla"><input type="checkbox" name="esTutor">Es tutor</label>`,
    });
  }
}

function direccion(datos, anterior = {}) {
  return {
    ...anterior,
    calle: datos.calle,
    numero: datos.numero,
    localidad: datos.localidad,
    provincia: datos.provincia,
    codigoPostal: datos.codigoPostal || null,
    complemento: datos.complemento || null,
  };
}
const requerido = (valor, mensaje) => {
  if (!String(valor ?? "").trim()) throw new Error(mensaje);
  return valor.trim();
};
const identificador = (valor) => {
  if (
    !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
      valor ?? "",
    )
  )
    throw new Error("El identificador del registro no es válido.");
  return valor;
};

export async function atenderFormulario(formulario, ctx) {
  if (await atenderFormularioOrganizacion(formulario, ctx)) return;
  const estado = ctx.estado;
  const nombre = formulario.dataset.formulario;
  const datosFormulario = new FormData(formulario);
  const datos = Object.fromEntries(datosFormulario);
  if (nombre === "buscar") {
    ctx.estado.filtros = Object.fromEntries(
      Object.entries(datos).filter(([, valor]) => valor.trim()),
    );
    ctx.estado.pagina = 1;
    await ctx.actualizar();
    return;
  }
  if (nombre === "buscar-lista-espera") {
    const { busqueda, sala, turnoPreferidoId } = datos;
    ctx.estado.filtrosEspera = { busqueda: busqueda.trim(), sala, turnoPreferidoId };
    ctx.estado.pagina = 1;
    await ctx.actualizar();
    return;
  }
  if (ctx.estado.previa) {
    ctx.notificar("Vista previa: los cambios no se guardan.");
    return;
  }
  const boton = formulario.querySelector(
    'button[type="submit"], button:not([type])',
  );
  const error = formulario.querySelector("[data-error]");
  if (formulario.dataset.guardando === "true") return;
  formulario.dataset.guardando = "true";
  if (boton) boton.disabled = true;
  if (error) error.hidden = true;
  const operacion = ctx.operacion;
  try {
    if (!ctx.estado.usuario) throw new Error("Iniciá sesión para continuar.");
    const ruta = ctx.estado.institucion ? base(ctx.estado) : null;
    if (!ruta && nombre !== "institucion" && !ctx.operacionOrganizacion?.permiteSinContexto)
      throw new Error("Seleccioná una institución antes de continuar.");
    const crear = (ruta, datos) => ctx.api(ruta, { metodo: "POST", datos });
    const actualizar = (ruta, datos) => ctx.api(ruta, { metodo: "PUT", datos });
    const clave = claveBorradores(ctx.estado);
    if (nombre === "aspirante") {
      if (estado.edadesAdmitidasPeriodo && estado.fechaCortePeriodo && !fechaNacimientoAdmitida(datos.fechaNacimiento, estado.fechaCortePeriodo, estado.edadesAdmitidasPeriodo))
        throw new Error(`La fecha de nacimiento no corresponde a las edades admitidas para este ciclo. Revisá el rango indicado; se calcula al ${estado.fechaCortePeriodo.split("-").reverse().join("/")}.`);
      const aspirante = await crear(`${ruta}/aspirantes`, {
        periodoId: estado.periodo.id,
        nombre: datos.nombre,
        apellido: datos.apellido,
        fechaNacimiento: datos.fechaNacimiento,
        dni: datos.dni || null,
        tipoDocumento: datos.tipoDocumento || null,
        sexo: datos.sexo || null,
      });
      const guardado = actualizarBorrador(clave, aspirante.id, true);
      if (clave !== claveBorradores(ctx.estado)) return;
      ctx.estado.borrador = {
        id: aspirante.id,
        nombre: datos.nombre,
        apellido: datos.apellido,
        fechaNacimiento: datos.fechaNacimiento,
      };
      ctx.estado.borradores = [
        ...(ctx.estado.borradores ?? []).filter((item) => item.id !== aspirante.id),
        ctx.estado.borrador,
      ];
      await ctx.actualizar();
      if (guardado === false)
        ctx.notificar("El navegador no pudo conservar el borrador. Guardá el identificador del aspirante para continuar más tarde.", true);
      else ctx.notificar("Aspirante registrado. Completá ahora su solicitud.");
      return;
    }
    if (nombre === "usar-aspirante") {
      const aspirante = await ctx.api(
        `${ruta}/aspirantes/${encodeURIComponent(identificador(datos.aspiranteId))}`,
      );
      const guardado = actualizarBorrador(clave, aspirante.id, true);
      if (clave !== claveBorradores(ctx.estado)) return;
      ctx.estado.borrador = aspirante;
      ctx.estado.borradores = [
        ...(ctx.estado.borradores ?? []).filter((item) => item.id !== aspirante.id),
        aspirante,
      ];
      await ctx.actualizar();
      if (guardado === false)
        ctx.notificar("El navegador no pudo conservar el borrador. Guardá el identificador del aspirante para continuar más tarde.", true);
      return;
    }
    if (nombre === "solicitud") {
      if (!ctx.estado.borrador)
        throw new Error("Primero registrá o seleccioná un aspirante.");
      if (!estado.fechaCortePeriodo || !estado.edadesAdmitidasPeriodo?.length)
        throw new Error("Todavía no se puede validar la edad: este período necesita una fecha de corte y reglas de inscripción configuradas.");
      if (!fechaNacimientoAdmitida(ctx.estado.borrador.fechaNacimiento, estado.fechaCortePeriodo, estado.edadesAdmitidasPeriodo))
        throw new Error(`La fecha de nacimiento no corresponde a las edades admitidas para este ciclo. Se aceptan ${estado.edadesAdmitidasPeriodo.join(", ")} años cumplidos al ${estado.fechaCortePeriodo.split("-").reverse().join("/")}.`);
      const aspiranteId = ctx.estado.borrador.id;
      const alternativas = datosFormulario.getAll("alternativas");
      if (alternativas.includes(datos.turnoPreferidoId))
        throw new Error(
          "El turno preferido no debe repetirse como alternativa.",
        );
      const solicitud = await crear(
        `${ruta}/periodos/${encodeURIComponent(ctx.estado.periodo.id)}/solicitudes`,
        {
          aspiranteId,
          domicilio: direccion(datos),
          turnoPreferidoId: datos.turnoPreferidoId,
          turnosAlternativosAceptados: alternativas,
          referenciaConsentimiento: datos.referenciaConsentimiento || null,
          articulacionPedagogica: datos.articulacionPedagogica,
          institucionProcedencia: datos.institucionProcedencia || null,
          hermanosGrupoConviviente: datos.hermanosGrupoConviviente,
          detalleHermanoGrupo: datos.detalleHermanoGrupo || null,
          hijoPersonal: datos.hijoPersonal,
          detalleHijoPersonal: datos.detalleHijoPersonal || null,
        },
      );
      actualizarBorrador(clave, aspiranteId, false);
      if (clave !== claveBorradores(ctx.estado)) return;
      ctx.estado.borradores = (ctx.estado.borradores ?? []).filter(
        (item) => item.id !== aspiranteId,
      );
      ctx.estado.borrador = null;
      ctx.notificar(
        solicitud.estadoRecepcion === "Ordinaria"
          ? "Solicitud registrada."
          : "Solicitud registrada fuera de término, pendiente de autorización.",
      );
      ctx.navegar(`solicitud/${solicitud.id}`);
      return;
    }
    if (nombre === "editar-aspirante") {
      const aspirante = operacion?.ficha?.persona;
      if (!aspirante || operacion.institucion?.id !== ctx.estado.institucion?.id)
        throw new Error("El contexto cambió. Cerrá el formulario y consultá nuevamente.");
      if (!operacion.edadesAdmitidas?.length || !operacion.fechaCorte)
        throw new Error("No se pudieron consultar las reglas históricas de edad de esta solicitud.");
      if (!fechaNacimientoAdmitida(datos.fechaNacimiento, operacion.fechaCorte, operacion.edadesAdmitidas))
        throw new Error(`La fecha de nacimiento no corresponde a las edades admitidas para este ciclo. Se aceptan ${operacion.edadesAdmitidas.join(", ")} años cumplidos al ${operacion.fechaCorte.split("-").reverse().join("/")}.`);
      await ctx.api(
        `${ruta}/aspirantes/${encodeURIComponent(aspirante.id)}`,
        {
          metodo: "PUT",
          datos: {
            solicitudId: operacion.ficha.solicitud.id,
            revisionSolicitud: operacion.ficha.solicitud.revisionDatos,
            nombre: datos.nombre,
            apellido: datos.apellido,
            fechaNacimiento: datos.fechaNacimiento,
            dni: datos.dni || null,
            tipoDocumento: datos.tipoDocumento || null,
            sexo: datos.sexo || null,
          },
        },
      );
      ctx.cerrarDialogo();
      await ctx.actualizar();
      ctx.notificar("Datos del aspirante actualizados.");
      return;
    }
    if (!operacion || operacion.institucion?.id !== ctx.estado.institucion?.id)
      throw new Error(
        "El contexto cambió. Cerrá el formulario y consultá nuevamente.",
      );
    const f = operacion.ficha;
    if (nombre === "institucion" || nombre === "editar-institucion") {
      if ((datos.latitud === "") !== (datos.longitud === ""))
        throw new Error("Completá ambas coordenadas o dejá las dos vacías.");
      const institucion = {
        nombre: datos.nombre,
        codigo: datos.codigo || null,
        telefonoContacto: datos.telefonoContacto,
        correoElectronicoContacto: datos.correoElectronicoContacto || null,
        domicilio: direccion(
          datos,
          nombre === "editar-institucion"
            ? operacion.institucion.domicilio
            : {},
        ),
        coordenadas:
          datos.latitud === ""
            ? null
            : {
                latitud: Number(datos.latitud),
                longitud: Number(datos.longitud),
              },
      };
      if (nombre === "editar-institucion")
        await actualizar(ruta, {
          ...institucion,
          revisionDatos: operacion.institucion.revisionDatos,
          motivo: datos.motivo,
        });
      else {
        const nueva = await crear("/instituciones", institucion);
        await ctx.recargarContexto({ institucionId: nueva.id });
      }
    } else if (nombre === "periodo") {
      const ciclo = Number(datos.ciclo);
      if (ctx.estado.periodos.some((periodo) => periodo.ciclo === ciclo))
        throw new Error(
          `Ya existe un período para el ciclo ${ciclo} en esta institución. Seleccioná el período existente desde el encabezado.`,
        );
      if (datos.fin < datos.inicio)
        throw new Error("La fecha final no puede ser anterior a la inicial.");
      const periodo = await crear(`${ruta}/periodos`, {
        ciclo,
        inicio: datos.inicio,
        fin: datos.fin,
      });
      await ctx.recargarContexto({ periodoId: periodo.id });
    } else if (nombre === "recepcion") {
      const p = operacion.periodo;
      await crear(
        `/periodos/${encodeURIComponent(p.id)}/${p.estado === "Preparado" ? "abrir" : "cerrar-recepcion"}`,
        { revision: p.revision, motivo: datos.motivo },
      );
    } else if (nombre === "nueva-aula" || nombre === "nuevo-turno") {
      await crear(`${ruta}/${nombre === "nueva-aula" ? "aulas" : "turnos"}`, {
        nombre: datos.nombre,
      });
    } else if (nombre === "oferta-institucional") {
      await crear(`${ruta}/ofertas-reutilizables`, {
        aulaId: datos.aulaId,
        turnoId: datos.turnoId,
        capacidad: Number(datos.capacidad),
        referenciaCapacidad: datos.referenciaCapacidad || null,
        motivo: datos.motivo,
      });
      operacion.mensajeExito = "Oferta guardada en el catálogo de la institución. Ya podés agregarla a los ciclos que la necesiten.";
    } else if (nombre === "editar-oferta-institucional") {
      const oferta = operacion.ofertaInstitucional;
      if (!oferta) throw new Error("No se encontró la oferta reutilizable.");
      await actualizar(`/ofertas-reutilizables/${encodeURIComponent(oferta.id)}`, {
        aulaId: datos.aulaId,
        turnoId: datos.turnoId,
        capacidad: Number(datos.capacidad),
        referenciaCapacidad: datos.referenciaCapacidad || null,
        motivo: datos.motivo,
      });
      operacion.mensajeExito = "Oferta reutilizable actualizada. Los ciclos ya configurados mantienen sus valores.";
    } else if (nombre === "agregar-oferta-periodo") {
      const plantilla = operacion.ofertaPlantilla;
      const periodo = operacion.periodo;
      if (!plantilla || !periodo)
        throw new Error("El contexto cambió. Cerrá el formulario y volvé a seleccionar la oferta y el período.");
      if (periodo.estado === "RecepcionAbierta" && periodo.versionReglasId && !datosFormulario.has("confirmarPublicacion"))
        throw new Error("Confirmá que querés aplicar este cambio a las nuevas solicitudes.");
      await crear(
        `/periodos/${encodeURIComponent(periodo.id)}/ofertas`,
        {
          aulaId: plantilla.aulaId,
          turnoId: plantilla.turnoId,
          capacidad: Number(datos.capacidad),
          disponible: datosFormulario.has("disponible"),
          referenciaCapacidad: plantilla.referenciaCapacidad || "Sin referencia informada",
          origenOcupaciones: "Sin ocupaciones iniciales",
          ocupaciones: [],
          motivo: datos.motivo,
        },
      );
      operacion.ofertaGuardada = true;
      const publicada = await publicarOfertaDelCiclo(ctx, operacion, datos.motivo);
      operacion.mensajeExito = publicada
        ? "Oferta agregada y aplicada automáticamente al ciclo. Ya está disponible para las nuevas solicitudes."
        : "Oferta agregada al ciclo. Se publicará al completar la configuración inicial con fecha de corte y reglas.";
    } else if (nombre === "editar-oferta-periodo") {
      const oferta = operacion.ofertaPeriodo;
      if (!oferta || operacion.periodo?.id !== ctx.estado.periodo?.id)
        throw new Error("El período cambió. Cerrá el formulario y consultá nuevamente.");
      if (operacion.periodo.estado === "RecepcionAbierta" && operacion.periodo.versionReglasId && !datosFormulario.has("confirmarPublicacion"))
        throw new Error("Confirmá que querés aplicar este cambio a las nuevas solicitudes.");
      await actualizar(`/ofertas/${encodeURIComponent(oferta.id)}`, {
        capacidad: Number(datos.capacidad),
        disponible: datosFormulario.has("disponible"),
        referenciaCapacidad: oferta.referenciaCapacidad,
        origenOcupaciones: oferta.origenOcupaciones,
        ocupaciones: oferta.ocupaciones,
        revision: oferta.revision,
        motivo: datos.motivo,
      });
      operacion.ofertaGuardada = true;
      const publicada = await publicarOfertaDelCiclo(ctx, operacion, datos.motivo);
      operacion.mensajeExito = publicada
        ? "Oferta actualizada y aplicada automáticamente al ciclo."
        : "Oferta del ciclo actualizada. Se publicará al completar la configuración inicial.";
    } else if (nombre === "preparar-ciclo") {
      const r = `/periodos/${encodeURIComponent(operacion.periodo.id)}`;
      const referencia = `Configuración inicial del ciclo ${operacion.periodo.ciclo}`;
      operacion.versiones ??= {};
      if (!operacion.periodo.versionReglasId && !operacion.versiones.reglas)
        operacion.versiones.reglas = (
          await crear(`${r}/versiones-reglas`, {
            fechaCorte: datos.fechaCorte,
            referencia,
            motivo: datos.motivo,
          })
        ).id;
      if (!operacion.periodo.versionOfertaId && !operacion.versiones.oferta)
        operacion.versiones.oferta = (
          await crear(`${r}/versiones-oferta`, {
            referencia,
            motivo: datos.motivo,
          })
        ).id;
      await actualizar(`${r}/configuracion`, {
        versiones: {
          versionReglasId: operacion.versiones.reglas ?? operacion.periodo.versionReglasId,
          versionOfertaId: operacion.versiones.oferta ?? operacion.periodo.versionOfertaId,
        },
        revision: operacion.periodo.revision,
        motivo: datos.motivo,
      });
      operacion.mensajeExito = "Ciclo preparado. La fecha de corte y la oferta quedaron asociadas a este período.";
    } else if (nombre === "editar-fecha-corte") {
      const r = `/periodos/${encodeURIComponent(operacion.periodo.id)}`;
      const referencia = `Actualización de fecha de corte del ciclo ${operacion.periodo.ciclo}`;
      operacion.versiones ??= {};
      if (!operacion.versiones.reglas)
        operacion.versiones.reglas = (
          await crear(`${r}/versiones-reglas`, {
            fechaCorte: datos.fechaCorte,
            referencia,
            motivo: datos.motivo,
          })
        ).id;
      await actualizar(`${r}/configuracion`, {
        versiones: {
          versionReglasId: operacion.versiones.reglas,
          versionOfertaId: operacion.periodo.versionOfertaId,
        },
        revision: operacion.periodo.revision,
        motivo: datos.motivo,
      });
      operacion.mensajeExito = "Fecha de corte actualizada para el ciclo. La oferta publicada no se modificó.";
    } else if (nombre === "publicar-oferta-periodo") {
      if (operacion.periodo.estado === "RecepcionAbierta" && !datosFormulario.has("confirmarPublicacion"))
        throw new Error("Confirmá que querés aplicar este cambio a las nuevas solicitudes.");
      const r = `/periodos/${encodeURIComponent(operacion.periodo.id)}`;
      const referencia = `Aplicación de oferta del ciclo ${operacion.periodo.ciclo}`;
      operacion.versiones ??= {};
      if (!operacion.versiones.oferta)
        operacion.versiones.oferta = (
          await crear(`${r}/versiones-oferta`, {
            referencia,
            motivo: datos.motivo,
          })
        ).id;
      await actualizar(`${r}/configuracion`, {
        versiones: {
          versionReglasId: operacion.periodo.versionReglasId,
          versionOfertaId: operacion.versiones.oferta,
        },
        revision: operacion.periodo.revision,
        motivo: datos.motivo,
      });
      operacion.mensajeExito = "Oferta publicada para el ciclo. La fecha de corte y las reglas se conservaron.";
    } else if (nombre === "distancia") {
      if (
        !Number.isFinite(Number(datos.distanciaMetros)) ||
        Number(datos.distanciaMetros) < 0
      )
        throw new Error("Indicá una distancia no negativa en metros.");
      await ctx.api(`${f.rutaSolicitud}/distancias/manual`, {
        metodo: "POST",
        datos: {
          distanciaMetros: datos.distanciaMetros,
          referencia: datos.referencia,
          revisionDomicilio: f.solicitud.revisionDomicilio,
          revisionUbicacionJardin: operacion.institucion.revisionUbicacion,
          ultimoNumeroRegistro: f.distancias[0]?.numeroRegistro ?? 0,
        },
        decimales: { distanciaMetros: datos.distanciaMetros },
      });
    } else if (nombre === "revision-domicilio") {
      if (datos.estado === "RevisadoManualmente")
        requerido(
          datos.referencia,
          "Agregá una referencia de la comprobación manual.",
        );
      await crear(`${f.rutaSolicitud}/revision-domicilio`, {
        estado: datos.estado,
        motivo: datos.motivo,
        referencia: datos.referencia || null,
        revisionSolicitud: f.revision.revisionSolicitud,
        revisionDomicilio: f.revision.revisionDomicilio,
        ultimoNumeroRegistro: f.revision.ultimoNumeroRegistro,
      });
    } else if (nombre === "corregir") {
      await actualizar(f.rutaSolicitud, {
        domicilio: direccion(datos, f.solicitud.domicilio),
        turnoPreferidoId: datos.turnoPreferidoId,
        turnosAlternativosAceptados: f.solicitud.turnosAlternativosAceptados,
        revisionSolicitud: f.solicitud.revisionDatos,
        motivo: datos.motivo,
        referenciaConsentimiento: datos.referenciaConsentimiento || null,
      });
    } else if (nombre === "duplicado") {
      await crear(
        `${ruta}/duplicados/${encodeURIComponent(operacion.id)}/resolver`,
        {
          decision: datos.decision,
          revision: operacion.alerta.revision,
          motivo: datos.motivo,
        },
      );
    } else if (nombre === "corregir-prioridad") {
      if (estado.periodo?.estado !== "RecepcionAbierta")
        throw new Error("La respuesta solo se puede cambiar mientras la recepción esté abierta.");
      const condicion = f.antecedentes.condiciones.find((x) => x.id === operacion.id);
      if (!condicion) throw new Error("No se encontró la declaración de prioridad.");
      const grupo = {
        ArticulacionPedagogica: "articulacion",
        DeclaracionArticulacionNegativa: "articulacion",
        ConsultaArticulacionPedagogica: "articulacion",
        HermanosYGruposConvivientes: "hermanos",
        DeclaracionHermanosNegativa: "hermanos",
        HijosDelPersonal: "personal",
        DeclaracionHijosPersonalNegativa: "personal",
      }[condicion.tipo];
      const respuestas = {
        articulacion: datos.respuestaArticulacion,
        hermanos: datos.respuestaHermanosGrupo,
        personal: datos.respuestaHijoPersonal,
      };
      const detalles = {
        articulacion: datos.institucionProcedencia,
        hermanos: datos.detalleHermanoGrupo,
        personal: datos.detalleHijoPersonal,
      };
      const respuesta = respuestas[grupo];
      const detalle = detalles[grupo]?.trim();
      if (!grupo) throw new Error("No se reconoce el criterio de prioridad.");
      const definiciones = {
        articulacion: {
          Si: ["ArticulacionPedagogica", "La familia declara que la institución de procedencia articula formalmente con este jardín."],
          No: ["DeclaracionArticulacionNegativa", "La familia responde que no hay una articulación formal conocida con este jardín."],
          NoEstoySeguro: ["ConsultaArticulacionPedagogica", "La familia no sabe si la institución de procedencia articula formalmente con este jardín."],
        },
        hermanos: {
          Si: ["HermanosYGruposConvivientes", "La familia declara que el aspirante tiene un hermano o integrante de su grupo conviviente en el jardín."],
          No: ["DeclaracionHermanosNegativa", "La familia declara que el aspirante no tiene un hermano o integrante de su grupo conviviente en el jardín."],
        },
        personal: {
          Si: ["HijosDelPersonal", "La familia declara que el aspirante es hijo/a de personal del jardín o de una institución que articula con él."],
          No: ["DeclaracionHijosPersonalNegativa", "La familia declara que el aspirante no es hijo/a de personal del jardín ni de una institución articulada."],
        },
      };
      const [tipo, valorDeclarado] = definiciones[grupo]?.[respuesta] ?? [];
      if (!tipo) throw new Error("Seleccioná una respuesta válida.");
      if ((grupo === "articulacion" && ["Si", "NoEstoySeguro"].includes(respuesta) || grupo !== "articulacion" && respuesta === "Si") && !detalle)
        throw new Error("Completá el dato adicional para revisar esta declaración.");
      const evidenciaReferencia = grupo === "articulacion"
        ? detalle ? `Institución de procedencia informada por la familia: ${detalle}` : "Institución de procedencia no informada por la familia."
        : respuesta === "Si" ? detalle : "Respuesta de la familia al registrar la solicitud.";
      await actualizar(`${f.rutaSolicitud}/condiciones/${encodeURIComponent(condicion.id)}/declaracion`, {
        tipo,
        valorDeclarado,
        evidenciaReferencia,
        revisionSolicitud: f.antecedentes.revisionSolicitud,
        motivo: datos.motivo,
      });
    } else if (
      [
        "nuevo-documento",
        "revisar-documento",
        "nueva-condicion",
        "revisar-condicion",
      ].includes(nombre)
    ) {
      const documento = nombre.includes("documento");
      const nuevo = nombre.startsWith("nuev");
      const entrada = {
        tipo: datos.tipo,
        estado: datos.estado,
        motivo: datos.motivo,
        referencia: datos.referencia || null,
        evidenciaReferencia: datos.referencia || null,
        observacion: datos.observacion || null,
        revisionSolicitud: f.antecedentes.revisionSolicitud,
        valorDeclarado: datos.valorDeclarado,
      };
      const url = `${f.rutaSolicitud}/${documento ? "documentos" : "condiciones"}${nuevo ? "" : `/${encodeURIComponent(operacion.id)}`}`;
      if (nuevo) await crear(url, entrada);
      else await actualizar(url, entrada);
    } else if (nombre === "responsable") {
      let id = datos.responsableId || operacion.responsableCreado;
      if (!id) {
        requerido(datos.nombre, "Completá el nombre del responsable.");
        requerido(datos.apellido, "Completá el apellido del responsable.");
        requerido(datos.telefono, "Completá el teléfono del responsable.");
        id = (
          await crear(`${ruta}/responsables`, {
            nombre: datos.nombre,
            apellido: datos.apellido,
            telefono: datos.telefono,
            documento: datos.documento || null,
            correoElectronico: datos.correoElectronico || null,
          })
        ).id;
        operacion.responsableCreado = id;
      }
      await crear(
        `${ruta}/aspirantes/${encodeURIComponent(f.solicitud.aspiranteId)}/responsables`,
        {
          responsableId: identificador(id),
          tipoVinculo: datos.tipoVinculo,
          contactoPrincipal: datosFormulario.has("contactoPrincipal"),
          esTutor: datosFormulario.has("esTutor"),
        },
      );
    } else throw new Error("Esta operación todavía no está disponible.");
    ctx.cerrarDialogo();
    await ctx.recargarContexto();
    await ctx.actualizar();
    ctx.notificar(operacion?.mensajeExito ?? "Los datos se guardaron correctamente.");
  } catch (problema) {
    if (operacion?.ofertaGuardada) {
      ctx.cerrarDialogo();
      ctx.notificar(`La oferta quedó guardada, pero no se pudo publicar automáticamente. ${problema.message} Los cambios pendientes quedan disponibles para aplicar desde Períodos.`, true);
      try {
        await ctx.recargarContexto();
        await ctx.actualizar();
      } catch (errorRecarga) {
        console.error("No se pudo actualizar la vista después de guardar la oferta.", errorRecarga);
      }
      return;
    }
    let mensaje = problema.message;
    if (problema.estado === 409)
      mensaje +=
        " Cerrá el formulario y usá “Intentar nuevamente” o volvé a abrir la ficha para consultar la revisión actual.";
    if (operacion?.responsableCreado)
      mensaje += ` El responsable ya fue creado: ${operacion.responsableCreado}. Podés reintentar su vinculación.`;
    if (operacion?.versiones)
      mensaje += ` Publicaciones conservadas: reglas ${operacion.versiones.reglas ?? "pendiente"}, oferta ${operacion.versiones.oferta ?? "pendiente"}.`;
    if (error?.isConnected) {
      error.textContent = mensaje;
      error.hidden = false;
      error.focus();
    } else ctx.notificar(mensaje, true);
  } finally {
    formulario.dataset.guardando = "false";
    if (boton?.isConnected) boton.disabled = false;
  }
}
