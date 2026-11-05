import {
  escapar as e,
  icono,
  etiqueta,
  fecha,
  jardinIlustrado,
  textoEstado,
} from "./interfaz.js";
import {
  encabezado,
  tarjeta,
  buscarSolicitudes,
  tablaSolicitudes,
} from "./compartidos.js";
export async function inicio(ctx) {
  const { estado } = ctx;
  const consulta = estado.periodo
    ? await buscarSolicitudes(ctx)
    : { resultados: [], hayMas: false };
  const indicadores = [
    [
      "solicitudes",
      "Solicitudes consultadas",
      `${consulta.resultados.length}${consulta.hayMas ? "+" : ""}`,
      "Registros de la consulta actual",
    ],
    [
      "personas",
      "Sala de 5 años",
      consulta.resultados.filter((s) => s.sala === 5).length,
      "En la consulta actual",
    ],
    [
      "personas",
      "Sala de 4 años",
      consulta.resultados.filter((s) => s.sala === 4).length,
      "En la consulta actual",
    ],
    [
      "personas",
      "Sala de 3 años",
      consulta.resultados.filter((s) => s.sala === 3).length,
      "En la consulta actual",
    ],
  ];
  return (
    encabezado({
      titulo: `Buen día, ${estado.usuario.nombre.split(" ")[0]}.`,
      descripcion: "Un nuevo día para acompañar cada comienzo.",
      accion:
        '<a class="boton" href="#nueva-solicitud">' +
        icono("mas") +
        "Nueva solicitud</a>",
    }) +
    `<section class="bienvenida"><div class="bienvenida__texto"><p class="bienvenida__epigrafe">Tu comunidad, más cerca</p><h2 class="bienvenida__titulo">Cada inscripción abre una posibilidad.</h2><p class="bienvenida__descripcion">Organizá las solicitudes, acompañá a las familias y mantené la información de tu jardín en un mismo lugar.</p>${estado.periodo ? etiqueta(estado.periodo.estado) : '<a href="#periodos" class="boton boton--pequeno">Preparar un período</a>'}</div><div class="bienvenida__imagen">${jardinIlustrado()}</div></section>` +
    `<section class="indicadores" aria-label="Resumen de solicitudes">${indicadores.map(([dibujo, nombre, valor, detalle]) => `<article class="indicador"><div class="indicador__cabecera"><span>${nombre}</span><span class="indicador__icono">${icono(dibujo)}</span></div><div class="indicador__numero">${valor}</div><div class="indicador__detalle">${detalle}</div></article>`).join("")}</section>` +
    `<div class="rejilla rejilla--principal"><div><section class="tarjeta"><div class="tarjeta__cabecera"><div><h2 class="tarjeta__titulo">Solicitudes del período</h2><p class="tarjeta__descripcion">Una mirada a las inscripciones de tu jardín.</p></div><a class="boton boton--texto boton--pequeno" href="#solicitudes">Ver todas ${icono("flecha")}</a></div>${tablaSolicitudes({ resultados: consulta.resultados.slice(0, 4), resumen: true })}</section><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Antes de procesar los ingresos</h2>${etiqueta("En preparación", "neutra")}</div><div class="tarjeta__cuerpo">${[
      [
        "Completá la información de cada solicitud",
        "Datos del aspirante, domicilio y preferencias.",
      ],
      [
        "Revisá documentación y posibles duplicados",
        "Cada decisión conserva su motivo e historial.",
      ],
      [
        "Prepará el período para la siguiente etapa",
        "Revisá prioridades y propuestas desde Vacantes y admisión.",
      ],
    ]
      .map(
        ([titulo, descripcion], i) =>
          `<div class="proceso"><span class="proceso__numero">${i + 1}</span><div><div class="proceso__texto">${titulo}</div><div class="proceso__detalle">${descripcion}</div></div></div>`,
      )
      .join("")}</div></section></div><div>${tarjeta({
      titulo: "Accesos rápidos",
      cuerpo: [
        [
          "nueva-solicitud",
          "mas",
          "Registrar solicitud",
          "Iniciá una nueva inscripción.",
        ],
        [
          "domicilios",
          "ubicacion",
          "Revisar domicilios",
          "Comprobaciones y distancias manuales.",
        ],
        [
          "oferta",
          "aulas",
          "Consultar oferta",
          "Aulas, turnos y capacidad inicial.",
        ],
      ]
        .map(
          ([ruta, dibujo, titulo, descripcion]) =>
            `<a class="acceso-rapido" href="#${ruta}"><span class="acceso-rapido__icono">${icono(dibujo)}</span><span class="acceso-rapido__texto"><span class="acceso-rapido__titulo">${titulo}</span><span class="acceso-rapido__descripcion">${descripcion}</span></span>${icono("flecha")}</a>`,
        )
        .join(""),
    })}<div class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Tu período</h2>${icono("calendario")}</div><div class="tarjeta__cuerpo">${estado.periodo ? `<dl class="datos"><div><dt>Ciclo lectivo</dt><dd>${e(estado.periodo.ciclo)}</dd></div><div><dt>Recepción</dt><dd>${e(textoEstado(estado.periodo.estado))}</dd></div><div><dt>Inicio previsto</dt><dd>${fecha(estado.periodo.inicio)}</dd></div><div><dt>Fin previsto</dt><dd>${fecha(estado.periodo.fin)}</dd></div></dl><hr class="separador"><p class="formulario__nota">La apertura y el cierre de la recepción se realizan manualmente.</p>` : "<p>Prepará tu primer período para comenzar.</p>"}</div></div></div></div>`
  );
}
