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
} from "./compartidos.js";
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
  const consulta = await buscarSolicitudes(ctx);
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
          "Las distancias se cargan manualmente y quedan identificadas como provisorias. La localización automática está pendiente.",
          "aviso",
        )
      : "") +
    `<section class="tarjeta"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">${modo === "solicitud" ? "Listado de solicitudes" : "Seleccioná una solicitud"}</h2>${etiqueta(`Ciclo ${ctx.estado.periodo.ciclo}`, "neutra")}</div><form class="filtros" data-formulario="buscar">${campo({ nombre: "nombre", etiqueta: "Nombre", valor: f.nombre ?? "", atributos: 'placeholder="Buscar por nombre"' })}${campo({ nombre: "apellido", etiqueta: "Apellido", valor: f.apellido ?? "", atributos: 'placeholder="Buscar por apellido"' })}${campo({ nombre: "dni", etiqueta: "DNI completo", valor: f.dni ?? "", atributos: 'placeholder="Sin puntos" inputmode="numeric"' })}<button class="boton boton--secundario" type="submit">${icono("buscar")}Buscar</button><button class="boton boton--texto" type="button" data-accion="limpiar-filtros">Limpiar</button></form>${tablaSolicitudes({ resultados: consulta.resultados, modo, borrador: ctx.estado.borrador, borradores: ctx.estado.borradores })}<div class="tarjeta__pie"><span>${consulta.resultados.length + (modo === "solicitud" ? ctx.estado.borradores.length : 0)} registros en esta página</span><div><button class="boton boton--secundario boton--pequeno" data-accion="pagina-anterior" ${ctx.estado.pagina <= 1 ? "disabled" : ""}>Anterior</button> <span>Página ${ctx.estado.pagina}</span> <button class="boton boton--secundario boton--pequeno" data-accion="pagina-siguiente" ${consulta.hayMas ? "" : "disabled"}>Siguiente</button></div></div></section>`
  );
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
    `<div class="rejilla rejilla--principal"><div>${tarjeta({ titulo: "1. Datos del aspirante", descripcion: "El DNI es opcional. No ingreses un documento ficticio.", cuerpo: borrador ? `${aviso(`Aspirante registrado: <strong>${e(borrador.nombre)} ${e(borrador.apellido)}</strong>.<br>Conservá su identificador si interrumpís la inscripción: <code>${e(borrador.id)}</code>`)}<p class="formulario__nota">Este aspirante se utilizará en el siguiente paso.</p>` : `<form class="formulario" data-formulario="aspirante"><div class="formulario__rejilla">${campo({ nombre: "nombre", etiqueta: "Nombre", obligatorio: true, atributos: 'autocomplete="off"' })}${campo({ nombre: "apellido", etiqueta: "Apellido", obligatorio: true, atributos: 'autocomplete="off"' })}${campo({ nombre: "fechaNacimiento", etiqueta: "Fecha de nacimiento", tipo: "date", obligatorio: true })}${campo({ nombre: "dni", etiqueta: "DNI (opcional)", atributos: 'inputmode="numeric"' })}</div><p class="formulario__error" data-error hidden role="alert"></p><div class="formulario__acciones"><button class="boton" type="submit" ${estado.previa ? "disabled" : ""}>Registrar aspirante ${icono("flecha")}</button></div></form><details class="acceso__detalle"><summary>Ya tengo un aspirante registrado</summary><form class="formulario" data-formulario="usar-aspirante">${campo({ nombre: "aspiranteId", etiqueta: "Identificador del aspirante", obligatorio: true, ayuda: "Usá el identificador de un registro anterior de esta institución." })}<p data-error class="formulario__error" hidden role="alert"></p><button class="boton boton--secundario" ${estado.previa ? "disabled" : ""}>Seleccionar aspirante</button></form></details>` })}<div class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">2. Domicilio y preferencias</h2><p class="tarjeta__descripcion">La edad, sala y situación de recepción las determina el sistema.</p></div></div><div class="tarjeta__cuerpo"><form class="formulario" data-formulario="solicitud"><fieldset class="formulario__seccion" ${borrador ? "" : "disabled"}><div class="formulario__rejilla">${camposDomicilio()}${seleccion({ nombre: "turnoPreferidoId", etiqueta: "Turno preferido", opciones: [{ id: "", nombre: "Seleccioná un turno" }, ...estado.turnosSolicitud], obligatorio: true })}${campo({ nombre: "referenciaConsentimiento", etiqueta: "Referencia de consentimiento (opcional)" })}</div><div class="campo"><span class="campo__etiqueta">Otros turnos aceptados por la familia</span>${estado.turnosSolicitud.map((t) => `<label class="casilla"><input type="checkbox" name="alternativas" value="${e(t.id)}">${e(t.nombre)}</label>`).join("")}</div><p class="formulario__nota">Seleccioná solo alternativas aceptadas y distintas del turno preferido.</p><p data-error class="formulario__error" hidden role="alert"></p><div class="formulario__acciones"><button class="boton" ${estado.previa ? "disabled" : ""}>Registrar solicitud ${icono("listo")}</button></div></fieldset></form></div></div></div><div>${tarjeta(
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
