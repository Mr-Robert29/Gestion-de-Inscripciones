import {
  escapar as e,
  icono,
  etiqueta,
  campo,
  seleccion,
  aviso,
} from "./interfaz.js";
import {
  encabezado,
  tarjeta,
  buscarSolicitudes,
  tablaSolicitudes,
  volver,
  base,
} from "./compartidos.js";
import { recuperarBorradores } from "./borradores-solicitud.js";
import { numero } from "./interfaz.js";
export async function solicitudes(ctx, modo = "solicitud") {
  const titulosModo = {
    solicitud: [
      "Solicitudes",
      "Consultá y acompañá las inscripciones de tu institución.",
    ],
    domicilio: [
      "Domicilios y distancias",
      "Revisá cada dirección y conservá sus mediciones manuales.",
    ],
    documentos: [
      "Documentación y condiciones",
      "Consultá los antecedentes registrados en cada solicitud.",
    ],
  };
  const [titulo, descripcion] = titulosModo[modo];
  const [consulta, borradores] = await Promise.all([
    buscarSolicitudes(ctx),
    modo === "solicitud" ? recuperarBorradores(ctx) : [],
  ]);
  let distancias = {};
  let revisionesDomicilio = {};
  if (modo === "domicilio" && !ctx.estado.previa) {
    const revisiones = await Promise.all(
      consulta.resultados.map(async (solicitud) => {
        const ruta = `${base(ctx.estado)}/solicitudes/${encodeURIComponent(solicitud.id)}`;
        const [historial, revision] = await Promise.all([
          ctx.api(`${ruta}/distancias`),
          ctx.api(`${ruta}/revision-domicilio`),
        ]);
        return [
          solicitud.id,
          {
            distancia: historial.find((medicion) => medicion.esUltimoRegistro) ?? historial[0] ?? null,
            domicilio: revision,
          },
        ];
      }),
    );
    const seguimiento = Object.fromEntries(revisiones);
    distancias = Object.fromEntries(Object.entries(seguimiento).map(([id, datos]) => [id, datos.distancia]));
    revisionesDomicilio = Object.fromEntries(Object.entries(seguimiento).map(([id, datos]) => [id, datos.domicilio]));
  }
  const f = ctx.estado.filtros;
  return (
    encabezado({
      titulo,
      descripcion,
      accion:
        modo === "solicitud"
          ? `<a class="boton" href="#nueva-solicitud/nuevo">${icono("mas")}Nueva solicitud</a>`
          : "",
    }) +
    (modo === "domicilio"
      ? aviso(
          "La distancia no se calcula automáticamente. “Manual provisoria” indica que fue ingresada por el equipo; “Desactualizada” indica que cambió el domicilio o la ubicación del jardín y hay que volver a medirla.",
          "aviso",
        )
      : "") +
    `<section class="tarjeta"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">${modo === "solicitud" ? "Listado de solicitudes" : modo === "domicilio" ? "Revisión de domicilios y distancias" : "Seleccioná una solicitud"}</h2>${modo === "domicilio" ? `<p class="tarjeta__descripcion">Resumen de las ${consulta.resultados.length} solicitudes de esta página</p>` : ""}</div>${etiqueta(`Ciclo ${ctx.estado.periodo.ciclo}`, "neutra")}</div>${modo === "domicilio" ? resumenDomicilios(consulta.resultados, distancias, revisionesDomicilio) : ""}<form class="filtros" data-formulario="buscar">${campo({ nombre: "nombre", etiqueta: "Nombre", valor: f.nombre ?? "", atributos: 'placeholder="Buscar por nombre"' })}${campo({ nombre: "apellido", etiqueta: "Apellido", valor: f.apellido ?? "", atributos: 'placeholder="Buscar por apellido"' })}${campo({ nombre: "dni", etiqueta: "DNI completo", valor: f.dni ?? "", atributos: 'placeholder="Sin puntos" inputmode="numeric"' })}<button class="boton boton--secundario" type="submit">${icono("buscar")}Buscar</button><button class="boton boton--texto" type="button" data-accion="limpiar-filtros">Limpiar</button></form>${modo === "domicilio" ? `<div class="domicilios__filtro"><label class="campo" for="filtro-seguimiento-domicilio"><span class="campo__etiqueta">Filtrar por estado</span><select id="filtro-seguimiento-domicilio" class="campo__control"><option value="todos">Todas las solicitudes</option><option value="domicilio-pendiente">Domicilio pendiente de revisión</option><option value="distancia-pendiente">Distancia pendiente de cargar</option><option value="distancia-manual">Distancia manual provisoria</option><option value="distancia-desactualizada">Distancia desactualizada</option></select></label></div>` : ""}${tablaSolicitudes({ resultados: consulta.resultados, modo, borradores, distancias, revisionesDomicilio })}<div class="tarjeta__pie"><span data-resultados-domicilio>${consulta.resultados.length + (modo === "solicitud" ? borradores.length : 0)} ${modo === "domicilio" ? "solicitudes visibles" : "registros"} en esta página</span><div><button class="boton boton--secundario boton--pequeno" data-accion="pagina-anterior" ${ctx.estado.pagina <= 1 ? "disabled" : ""}>Anterior</button> <span>Página ${ctx.estado.pagina}</span> <button class="boton boton--secundario boton--pequeno" data-accion="pagina-siguiente" ${consulta.hayMas ? "" : "disabled"}>Siguiente</button></div></div></section>`
  );
}

function resumenDomicilios(solicitudes, distancias, revisiones) {
  const cantidades = solicitudes.reduce((total, solicitud) => {
    const distancia = distancias[solicitud.id];
    const domicilio = revisiones[solicitud.id];
    if (!domicilio?.revisionActual || domicilio.estado === "PendienteRevision") total.domiciliosPendientes++;
    if (!distancia) total.distanciasPendientes++;
    else if (!distancia.esVigente) total.desactualizadas++;
    else total.manuales++;
    return total;
  }, { domiciliosPendientes: 0, distanciasPendientes: 0, manuales: 0, desactualizadas: 0 });
  const items = [
    ["Domicilios pendientes", cantidades.domiciliosPendientes, "de revisar"],
    ["Distancias pendientes", cantidades.distanciasPendientes, "de cargar"],
    ["Mediciones manuales", cantidades.manuales, "provisorias"],
    ["Desactualizadas", cantidades.desactualizadas, "para volver a medir"],
  ];
  return `<div class="domicilios__resumen">${items.map(([titulo, total, detalle]) => `<div class="domicilios__indicador"><span>${titulo}</span><strong>${total}</strong><small>${detalle}</small></div>`).join("")}</div>`;
}
export function camposDomicilio(d = {}) {
  return `${campo({ nombre: "calle", etiqueta: "Calle", valor: d.calle, obligatorio: true })}${campo({ nombre: "numero", etiqueta: "Número", valor: d.numero, obligatorio: true })}${campo({ nombre: "localidad", etiqueta: "Localidad", valor: d.localidad, obligatorio: true })}${campo({ nombre: "provincia", etiqueta: "Provincia", valor: d.provincia, obligatorio: true })}${campo({ nombre: "codigoPostal", etiqueta: "Código postal (opcional)", valor: d.codigoPostal })}${campo({ nombre: "complemento", etiqueta: "Complemento (opcional)", valor: d.complemento })}`;
}
export function nuevaSolicitud(ctx) {
  const { estado } = ctx;
  const borrador = estado.borrador;
  return (
    encabezado({
      titulo: "Nueva solicitud",
      descripcion:
        "Primero registrá al aspirante; después completá su solicitud.",
      accion: volver,
    }) +
    (estado.periodo.estado === "RecepcionCerrada"
      ? aviso(
          "La recepción está cerrada. La solicitud se registrará fuera de término, pendiente de autorización.",
          "aviso",
        )
      : "") +
    (estado.periodo.estado === "Preparado" ||
    !estado.periodo.versionReglasId ||
    !estado.periodo.versionOfertaId
      ? aviso(
          "El período necesita recepción habilitada y versiones de reglas y oferta asignadas para registrar solicitudes.",
          "aviso",
        )
      : "") +
    `<div class="rejilla rejilla--principal"><div>${tarjeta({ titulo: "1. Datos del aspirante", descripcion: "El DNI, el tipo de documento y el sexo son opcionales. No ingreses un documento ficticio.", cuerpo: borrador ? `${aviso(`Aspirante registrado: <strong>${e(borrador.nombre)} ${e(borrador.apellido)}</strong>.<br>Identificador del aspirante: <code>${e(borrador.id)}</code>`)}<p class="formulario__nota">Este aspirante se utilizará en el siguiente paso.</p>` : `<form class="formulario" data-formulario="aspirante"><div class="formulario__rejilla">${campo({ nombre: "nombre", etiqueta: "Nombre", obligatorio: true, atributos: 'autocomplete="off"' })}${campo({ nombre: "apellido", etiqueta: "Apellido", obligatorio: true, atributos: 'autocomplete="off"' })}${campo({ nombre: "fechaNacimiento", etiqueta: "Fecha de nacimiento", tipo: "date", obligatorio: true })}${campo({ nombre: "dni", etiqueta: "DNI (opcional)", atributos: 'inputmode="numeric"' })}${campo({ nombre: "tipoDocumento", etiqueta: "Tipo de documento (opcional)", sugerencias: ["DNI", "Pasaporte", "Otro documento"] })}${campo({ nombre: "sexo", etiqueta: "Sexo (opcional)" })}</div><p class="formulario__error" data-error hidden role="alert"></p><div class="formulario__acciones"><button class="boton" type="submit" ${estado.previa ? "disabled" : ""}>Registrar aspirante ${icono("flecha")}</button></div></form><details class="acceso__detalle"><summary>Ya tengo un aspirante registrado</summary><form class="formulario" data-formulario="usar-aspirante">${campo({ nombre: "aspiranteId", etiqueta: "Identificador del aspirante", obligatorio: true, ayuda: "Usá el identificador de un registro anterior de esta institución." })}<p data-error class="formulario__error" hidden role="alert"></p><button class="boton boton--secundario" ${estado.previa ? "disabled" : ""}>Seleccionar aspirante</button></form></details>` })}<div class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">2. Domicilio y preferencias</h2><p class="tarjeta__descripcion">La edad, sala y situación de recepción las determina el sistema.</p></div></div><div class="tarjeta__cuerpo"><form class="formulario" data-formulario="solicitud"><fieldset class="formulario__seccion" ${borrador ? "" : "disabled"}><div class="formulario__rejilla">${camposDomicilio()}${seleccion({ nombre: "turnoPreferidoId", etiqueta: "Turno preferido", opciones: [{ id: "", nombre: "Seleccioná un turno" }, ...estado.turnosSolicitud], obligatorio: true })}${campo({ nombre: "referenciaConsentimiento", etiqueta: "Referencia de consentimiento (opcional)" })}</div><div class="campo"><span class="campo__etiqueta">Otros turnos aceptados por la familia</span>${estado.turnosSolicitud.map((t) => `<label class="casilla"><input type="checkbox" name="alternativas" value="${e(t.id)}">${e(t.nombre)}</label>`).join("")}</div><p class="formulario__nota">Seleccioná solo alternativas aceptadas y distintas del turno preferido.</p><p data-error class="formulario__error" hidden role="alert"></p><div class="formulario__acciones"><button class="boton" ${estado.previa ? "disabled" : ""}>Registrar solicitud ${icono("listo")}</button></div></fieldset></form></div></div></div><div>${tarjeta(
      {
        titulo: "Una inscripción, paso a paso",
        cuerpo:
          [
            ["Identificá al aspirante", "Con o sin DNI."],
            ["Registrá su solicitud", "Dirección y turnos aceptados."],
            [
              "Completá sus antecedentes",
              "Responsables, documentación y condiciones.",
            ],
          ]
            .map(
              ([t, d], i) =>
                `<div class="proceso"><span class="proceso__numero">${i + 1}</span><div><div class="proceso__texto">${t}</div><div class="proceso__detalle">${d}</div></div></div>`,
            )
            .join("") +
          '<hr class="separador"><p class="formulario__nota">Registrar una solicitud no confirma un ingreso ni asigna una vacante.</p>',
      },
    )}</div></div>`
  );
}
