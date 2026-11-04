import {
  escapar as e,
  etiqueta,
  fecha,
  numero,
  direccionTexto,
} from "./interfaz.js";
import { encabezado, tarjeta, base, volver } from "./compartidos.js";
import { detalleEjemplo } from "./datos-previa.js";
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
  const cabecera = encabezado({
    titulo: `${persona.nombre} ${persona.apellido}`,
    descripcion: `Solicitud ${s.id} · Ciclo ${ctx.estado.periodos.find((p) => p.id === s.periodoId)?.ciclo ?? ""}`,
    accion: volver,
  });
  const pestanas = `<nav class="pestanas" aria-label="Secciones de la ficha">${[
    ["solicitud", "Datos de inscripción"],
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
  if (pestana === "domicilio") {
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
    contenido = `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Documentación", accion: '<button class="boton boton--texto boton--pequeno" data-accion="nuevo-documento">+ Registrar</button>', cuerpo: a.documentos.length ? a.documentos.map((d) => `<div class="acceso-rapido"><div class="acceso-rapido__texto"><strong class="acceso-rapido__titulo">${e(d.tipo)}</strong><span class="acceso-rapido__descripcion">${e(d.referencia ?? "Sin referencia")}</span></div>${etiqueta(d.estado)}<button class="boton boton--texto boton--pequeno" data-accion="revisar-documento" data-id="${e(d.id)}">Revisar</button></div>`).join("") : '<p class="formulario__nota">No hay documentos registrados. Esto no significa que el legajo esté completo.</p>' })}${tarjeta({ titulo: "Condiciones declaradas", accion: '<button class="boton boton--texto boton--pequeno" data-accion="nueva-condicion">+ Declarar</button>', cuerpo: a.condiciones.length ? a.condiciones.map((c) => `<div class="acceso-rapido"><div class="acceso-rapido__texto"><strong class="acceso-rapido__titulo">${e(c.tipo)}</strong><span class="acceso-rapido__descripcion">${e(c.valorDeclarado)}</span></div>${etiqueta(c.estadoVerificacion)}<button class="boton boton--texto boton--pequeno" data-accion="revisar-condicion" data-id="${e(c.id)}">Revisar</button></div>`).join("") : '<p class="formulario__nota">Sin condiciones declaradas. Declarar una condición no la verifica ni concede prioridad automáticamente.</p>' })}</div>`;
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
    contenido = `<div class="rejilla rejilla--dos">${tarjeta({ titulo: "Datos del aspirante", cuerpo: `<dl class="datos"><div><dt>Nombre</dt><dd>${e(persona.nombre)}</dd></div><div><dt>Apellido</dt><dd>${e(persona.apellido)}</dd></div><div><dt>Fecha de nacimiento</dt><dd>${fecha(persona.fechaNacimiento ?? s.fechaNacimientoUtilizada)}</dd></div><div><dt>DNI</dt><dd>${e(persona.dni || "Sin DNI declarado")}</dd></div><div><dt>Grupo etario</dt><dd>${s.sala ? `Sala de ${e(s.sala)} años` : "Pendiente de revisión de edad"}</dd></div><div><dt>Fecha de corte utilizada</dt><dd>${fecha(s.fechaCorteUtilizada)}</dd></div></dl>` })}${tarjeta({ titulo: "Datos de inscripción", cuerpo: `<dl class="datos"><div><dt>Recepción</dt><dd>${etiqueta(s.estadoRecepcion)}</dd></div><div><dt>Turno preferido</dt><dd>${e(ctx.estado.turnos.find((t) => t.id === s.turnoPreferidoId)?.nombre ?? "Turno registrado")}</dd></div><div class="campo--completo"><dt>Domicilio</dt><dd>${e(direccionTexto(s.domicilio))}</dd></div><div><dt>Fecha de registro</dt><dd>${fecha(s.fechaRegistro)}</dd></div><div><dt>Referencia de consentimiento</dt><dd>${e(s.referenciaConsentimiento ?? "No informada")}</dd></div></dl><div class="formulario__acciones"><button class="boton boton--secundario" data-accion="corregir-solicitud">Corregir datos de solicitud</button></div>` })}</div>`;
  }
  return cabecera + pestanas + contenido;
}
