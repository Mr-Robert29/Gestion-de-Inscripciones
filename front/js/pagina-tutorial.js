import { encabezado, tarjeta } from "./compartidos.js";

const paso = (numero, titulo, descripcion, enlaces, detalles = []) =>
  `<li class="proceso tutorial__paso"><span class="proceso__numero">${numero}</span><div class="tutorial__paso-contenido"><div class="proceso__texto">${titulo}</div><p class="proceso__detalle">${descripcion}</p>${detalles.length ? `<ol class="tutorial__subpasos">${detalles.map((detalle) => `<li>${detalle}</li>`).join("")}</ol>` : ""}<div class="formulario__acciones">${enlaces.map(([ruta, texto]) => `<a class="boton boton--secundario boton--pequeno" href="#${ruta}">${texto}</a>`).join("")}</div></div></li>`;

export function tutorial() {
  return (
    encabezado({
      titulo: "Tutorial de uso",
      descripcion:
        "Un recorrido recomendado para preparar el ciclo, gestionar las inscripciones y organizar los ingresos.",
    }) +
    `<div class="rejilla">${tarjeta({
      titulo: "Antes de comenzar",
      cuerpo:
        '<p>Iniciá sesión con una cuenta autorizada y seleccioná la institución y el período con el que vas a trabajar. Las acciones disponibles dependen de tus permisos.</p><p class="formulario__nota">La sesión se cierra al vencer el token o al recargar la página. Si volvés a entrar, seleccioná el contexto correcto antes de continuar.</p>',
    })}${tarjeta({
      titulo: "Recorrido del ciclo lectivo",
      cuerpo: `<ol class="tutorial__pasos">${paso(
        1,
        "Prepará la institución",
        "Revisá nombre, datos de contacto, domicilio y ubicación del jardín.",
        [["institucion", "Mi institución"]],
      )}${paso(
        2,
        "Configurá el período y la oferta",
        "Creá el período, cargá aulas, turnos, capacidades y ocupaciones iniciales. Publicá las reglas y la oferta, asigná ambas al período y abrí la recepción.",
        [
          ["periodos", "Períodos"],
          ["oferta", "Aulas, turnos y oferta"],
        ],
      )}${paso(
        3,
        "Registrá las solicitudes",
        "En Solicitudes, elegí Nueva solicitud. Registrá o seleccioná al aspirante y completá domicilio y preferencias de turno. Después podés agregar responsables desde su ficha.",
        [["solicitudes", "Solicitudes"]],
      )}${paso(
        4,
        "Revisá la información",
        "Consultá la ficha para corregir datos del aspirante o de la solicitud. Revisá documentación, domicilios y distancias, y resolvé posibles duplicados con el motivo correspondiente.",
        [
          ["solicitudes", "Fichas"],
          ["documentacion", "Documentación"],
          ["domicilios", "Domicilios y distancias"],
          ["duplicados", "Posibles duplicados"],
        ],
      )}${paso(
        5,
        "Cerrá la recepción y revisá el orden de prioridad",
        "Cuando ya no vayas a recibir ni corregir solicitudes para este ciclo, cerrá la etapa de inscripción y atendé los controles previos a la admisión.",
        [
          ["periodos", "Períodos"],
          ["vacantes/prioridades", "Prioridades y sorteo"],
          ["vacantes", "Propuestas"],
        ],
        [
          "En <strong>Períodos</strong>, buscá el ciclo correcto y elegí <strong>Cerrar recepción</strong>. Hacelo cuando la inscripción haya terminado: después del cierre ya no se pueden corregir los datos de aspirantes ni de solicitudes.",
          "En <strong>Vacantes y admisión → Prioridades y sorteo</strong>, revisá las salas, el orden, los criterios aplicados y las solicitudes que aparezcan pendientes.",
          "Registrá un sorteo solo para los grupos que lo requieran. Si aparece un caso de revisión manual, asigná una posición distinta a cada participante y escribí el motivo de la decisión.",
          "Cuando los casos estén resueltos, entrá en <strong>Propuestas</strong> para preparar los posibles ingresos.",
        ],
      )}${paso(
        6,
        "Prepará, revisá y confirmá los ingresos",
        "La propuesta organiza quién podría ingresar y quién quedaría en espera. Revisala completa antes de convertirla en ingresos definitivos.",
        [["vacantes", "Propuestas"]],
        [
          "En <strong>Vacantes y admisión → Propuestas</strong>, elegí <strong>Generar propuesta</strong> e indicá el motivo. El resultado todavía es una propuesta: no reserva ni confirma por sí sola una vacante.",
          "Abrí la propuesta y revisá cada aspirante, sala, turno y explicación. Con <strong>Revisar resultado</strong> podés proponer un ingreso —seleccionando la oferta disponible— o mantener a la persona en espera; registrá el motivo.",
          "Si el ingreso depende de un turno alternativo, registrá primero la aceptación de la familia y su constancia. Volvé a consultar la propuesta si el sistema avisa que los datos cambiaron.",
          "Cuando todo esté correcto y vigente, elegí <strong>Confirmar ingresos</strong>. Esta confirmación crea los ingresos definitivos; luego podés verlos en <strong>Ingresos y bajas</strong>.",
        ],
      )}${paso(
        7,
        "Organizá los ingresos en secciones",
        "Primero definí las secciones que ofrece el jardín; después prepará y confirmá qué ingresos van a cada una.",
        [
          ["secciones", "Secciones"],
          ["secciones/distribuciones", "Distribuciones"],
        ],
        [
          "En la pestaña <strong>Secciones</strong>, elegí <strong>Definir sección</strong> e indicá su nombre, aula, turno y edades admitidas. Una sección puede ser pura (una edad) o multiedad (por ejemplo, 3 y 4 años).",
          "En <strong>Distribuciones</strong>, elegí <strong>Generar distribución</strong>. El sistema propone una sección compatible para cada ingreso activo; generar la propuesta todavía no confirma las asignaciones.",
          "Abrí la distribución y revisá edad, turno y sección de cada ingreso. Si hace falta, modificá la asignación o dejala sin sección indicando el motivo.",
          "Cuando revises toda la lista, elegí <strong>Confirmar distribución</strong>. Esa acción guarda las secciones definitivas. Si una propuesta quedó desactualizada, generá o consultá otra antes de confirmar.",
        ],
      )}${paso(
        8,
        "Atendé la lista de espera y consultá los resultados",
        "Si se libera una vacante después de las admisiones, podés ofrecerla a una persona compatible de la lista de espera. Después consultá el historial del ciclo.",
        [
          ["vacantes/espera", "Lista de espera"],
          ["vacantes/ofrecimientos", "Ofrecimientos"],
          ["vacantes/ingresos", "Ingresos y bajas"],
          ["reportes", "Resultados e historial"],
        ],
        [
          "En <strong>Lista de espera</strong>, consultá las posiciones y los casos pendientes. El orden se conserva; no elijas una persona fuera de los candidatos que el sistema marca como compatibles.",
          "Cuando una familia informa que deja una vacante, registrá la baja en <strong>Ingresos y bajas</strong>. Si queda pendiente de coordinación, la plaza sigue ocupada hasta completar el trámite; podés reintentarlo desde allí.",
          "En <strong>Ofrecimientos</strong>, elegí <strong>Consultar candidatos</strong> y seleccioná el aula y turno vacantes. Después de revisar los candidatos compatibles, ofrecé la plaza a la persona correspondiente e indicá el motivo.",
          "El ofrecimiento reserva la plaza, pero aún no confirma el ingreso. Registrá la respuesta familiar; si acepta, completá <strong>Confirmar ingreso</strong>. Si rechaza o cancela, la reserva se libera. Rechazar un turno alternativo no elimina a la familia de la espera.",
          "Por último, en <strong>Resultados e historial</strong> revisá los totales, ingresos, bajas y cambios. Desde esa pantalla descargá los reportes CSV que tus permisos habiliten.",
        ],
      )}</ol>`,
    })}</div><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Si algo no coincide</h2></div><div class="tarjeta__cuerpo"><p>Si el sistema informa que los datos cambiaron o que una operación está desactualizada, cerrá el formulario, consultá nuevamente la ficha y verificá los datos antes de volver a guardar. Las distancias se cargan manualmente y se marcan como provisorias; una distancia pendiente no equivale a cero.</p><p class="formulario__nota">Algunas decisiones, como confirmar ingresos o distribuciones, requieren permisos de Directivo. Si no ves la acción, consultá con una persona que tenga ese permiso.</p><a class="boton boton--texto" href="#ayuda">Ver ayuda y límites de esta versión</a></div></section>`
  );
}
