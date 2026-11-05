import { atenderAccionOrganizacion, atenderFormularioOrganizacion } from "./acciones-organizacion.js";
import {
  escapar as e,
  campo,
  seleccion,
  aviso,
  errorFormulario,
} from "./interfaz.js";
import { base } from "./compartidos.js";
import { camposDomicilio } from "./pagina-solicitudes.js";
const opciones = (nombres) => nombres.map(([id, nombre]) => ({ id, nombre }));
const accionesFormulario = (previa) =>
  `${errorFormulario()}${previa ? '<p class="formulario__nota">Vista previa: los cambios no se guardan.</p>' : ""}<div class="formulario__acciones"><button class="boton boton--secundario" type="button" data-accion="cerrar-dialogo">Cancelar</button><button class="boton" type="submit" ${previa ? "disabled" : ""}>Guardar</button></div>`;
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
    contenido: `<form class="formulario" data-formulario="${datos.formulario}">${datos.contenido}${accionesFormulario(ctx.estado.previa)}</form>`,
  });
}
export async function atenderAccion(elemento, ctx) {
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
      contenido: `<div class="formulario__rejilla">${campo({ nombre: "nombre", etiqueta: "Nombre del jardín", valor: d.nombre, obligatorio: true, completo: true })}${campo({ nombre: "codigo", etiqueta: "Código institucional (opcional)", valor: d.codigo })}${campo({ nombre: "telefonoContacto", etiqueta: "Teléfono", valor: d.telefonoContacto, tipo: "tel", obligatorio: true })}${campo({ nombre: "correoElectronicoContacto", etiqueta: "Correo (opcional)", valor: d.correoElectronicoContacto, tipo: "email", completo: true })}${camposDomicilio(d.domicilio)}${campo({ nombre: "latitud", etiqueta: "Latitud (opcional)", tipo: "number", valor: d.coordenadas?.latitud, atributos: 'step="any" min="-90" max="90"' })}${campo({ nombre: "longitud", etiqueta: "Longitud (opcional)", tipo: "number", valor: d.coordenadas?.longitud, atributos: 'step="any" min="-180" max="180"' })}${d.id ? campo({ nombre: "motivo", etiqueta: "Motivo del cambio", obligatorio: true, completo: true }) : ""}</div>`,
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
        ) + campo({ nombre: "motivo", etiqueta: "Motivo", obligatorio: true }),
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
  if (accion === "nueva-oferta") {
    if (!estado.aulas.length || !estado.turnos.length)
      throw new Error("Primero registrá al menos un aula y un turno.");
    mostrar(ctx, {
      titulo: "Agregar oferta del período",
      formulario: "oferta",
      contenido: `<div class="formulario__rejilla">${seleccion({ nombre: "aulaId", etiqueta: "Aula", opciones: estado.aulas, obligatorio: true })}${seleccion({ nombre: "turnoId", etiqueta: "Turno", opciones: estado.turnos, obligatorio: true })}${campo({ nombre: "capacidad", etiqueta: "Capacidad declarada", tipo: "number", obligatorio: true, ayuda: "Cantidad máxima de lugares para esta aula y turno.", atributos: 'min="0" step="1"' })}${campo({ nombre: "referenciaCapacidad", etiqueta: "Referencia de capacidad (opcional)", ayuda: "Podés indicar un acta, planilla o norma que respalde la capacidad informada." })}</div><label class="casilla"><input type="checkbox" name="disponible" checked>Disponible en este período</label>${campo({ nombre: "motivo", etiqueta: "Motivo del registro", obligatorio: true, ayuda: "Por ejemplo: Carga inicial de la oferta del período 2027." })}`,
    });
    return;
  }
  if (accion === "preparar-versiones") {
    mostrar(ctx, {
      titulo: "Publicar y asignar configuración",
      descripcion:
        "Publicará reglas y una copia de la oferta; luego las asociará al período seleccionado.",
      formulario: "versiones",
      contenido:
        aviso(
          "Las publicaciones anteriores se conservan. Verificá la oferta y la fecha de corte antes de continuar.",
          "aviso",
        ) +
        campo({
          nombre: "fechaCorte",
          etiqueta: "Fecha de corte para calcular edades",
          tipo: "date",
          obligatorio: true,
        }) +
        campo({
          nombre: "referencia",
          etiqueta: "Referencia administrativa",
          obligatorio: true,
        }) +
        campo({ nombre: "motivo", etiqueta: "Motivo", obligatorio: true }),
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
        campo({
          nombre: "motivo",
          etiqueta: "Motivo de la decisión",
          obligatorio: true,
        }),
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
        campo({ nombre: "motivo", etiqueta: "Motivo", obligatorio: true }) +
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
      contenido: `<div class="formulario__rejilla">${camposDomicilio(s.domicilio)}${seleccion({ nombre: "turnoPreferidoId", etiqueta: "Turno preferido", opciones: estado.turnos, valor: s.turnoPreferidoId, obligatorio: true })}${campo({ nombre: "referenciaConsentimiento", etiqueta: "Referencia de consentimiento", valor: s.referenciaConsentimiento })}</div>${campo({ nombre: "motivo", etiqueta: "Motivo de la corrección", obligatorio: true })}<p class="formulario__nota">Se conservarán las alternativas de turno ya aceptadas. Los datos personales compartidos del aspirante se corrigen mediante un procedimiento posterior.</p>`,
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
        campo({ nombre: "motivo", etiqueta: "Motivo", obligatorio: true }),
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
    if (!ruta && nombre !== "institucion")
      throw new Error("Seleccioná una institución antes de continuar.");
    const crear = (ruta, datos) => ctx.api(ruta, { metodo: "POST", datos });
    const actualizar = (ruta, datos) => ctx.api(ruta, { metodo: "PUT", datos });
    if (nombre === "aspirante") {
      const aspirante = await crear(`${ruta}/aspirantes`, {
        nombre: datos.nombre,
        apellido: datos.apellido,
        fechaNacimiento: datos.fechaNacimiento,
        dni: datos.dni || null,
      });
      ctx.estado.borrador = {
        id: aspirante.id,
        nombre: datos.nombre,
        apellido: datos.apellido,
      };
      ctx.estado.borradores = [
        ...(ctx.estado.borradores ?? []).filter((item) => item.id !== aspirante.id),
        ctx.estado.borrador,
      ];
      await ctx.actualizar();
      ctx.notificar("Aspirante registrado. Completá ahora su solicitud.");
      return;
    }
    if (nombre === "usar-aspirante") {
      const aspirante = await ctx.api(
        `${ruta}/aspirantes/${encodeURIComponent(identificador(datos.aspiranteId))}`,
      );
      ctx.estado.borrador = aspirante;
      ctx.estado.borradores = [
        ...(ctx.estado.borradores ?? []).filter((item) => item.id !== aspirante.id),
        aspirante,
      ];
      await ctx.actualizar();
      return;
    }
    if (nombre === "solicitud") {
      if (!ctx.estado.borrador)
        throw new Error("Primero registrá o seleccioná un aspirante.");
      const alternativas = datosFormulario.getAll("alternativas");
      if (alternativas.includes(datos.turnoPreferidoId))
        throw new Error(
          "El turno preferido no debe repetirse como alternativa.",
        );
      const solicitud = await crear(
        `${ruta}/periodos/${encodeURIComponent(ctx.estado.periodo.id)}/solicitudes`,
        {
          aspiranteId: ctx.estado.borrador.id,
          domicilio: direccion(datos),
          turnoPreferidoId: datos.turnoPreferidoId,
          turnosAlternativosAceptados: alternativas,
          referenciaConsentimiento: datos.referenciaConsentimiento || null,
        },
      );
      ctx.estado.borradores = (ctx.estado.borradores ?? []).filter(
        (item) => item.id !== ctx.estado.borrador.id,
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
      if (datos.fin < datos.inicio)
        throw new Error("La fecha final no puede ser anterior a la inicial.");
      const periodo = await crear(`${ruta}/periodos`, {
        ciclo: Number(datos.ciclo),
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
    } else if (nombre === "oferta") {
      await crear(
        `/periodos/${encodeURIComponent(operacion.periodo.id)}/ofertas`,
        {
          aulaId: datos.aulaId,
          turnoId: datos.turnoId,
          capacidad: Number(datos.capacidad),
          disponible: datosFormulario.has("disponible"),
          referenciaCapacidad: datos.referenciaCapacidad || "Sin referencia informada",
          origenOcupaciones: "No informado",
          ocupaciones: [],
          motivo: datos.motivo,
        },
      );
    } else if (nombre === "versiones") {
      const r = `/periodos/${encodeURIComponent(operacion.periodo.id)}`;
      operacion.versiones ??= {};
      if (!operacion.versiones.reglas)
        operacion.versiones.reglas = (
          await crear(`${r}/versiones-reglas`, {
            fechaCorte: datos.fechaCorte,
            referencia: datos.referencia,
            motivo: datos.motivo,
          })
        ).id;
      if (!operacion.versiones.oferta)
        operacion.versiones.oferta = (
          await crear(`${r}/versiones-oferta`, {
            referencia: datos.referencia,
            motivo: datos.motivo,
          })
        ).id;
      await actualizar(`${r}/configuracion`, {
        versiones: {
          versionReglasId: operacion.versiones.reglas,
          versionOfertaId: operacion.versiones.oferta,
        },
        revision: operacion.periodo.revision,
        motivo: datos.motivo,
      });
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
    ctx.notificar("Los datos se guardaron correctamente.");
  } catch (problema) {
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
