import { encabezado, tarjeta } from "./compartidos.js";

const paso = (numero, titulo, descripcion, enlaces, detalles = []) =>
  `<li class="proceso tutorial__paso"><span class="proceso__numero">${numero}</span><div class="tutorial__paso-contenido"><div class="proceso__texto">${titulo}</div><p class="proceso__detalle">${descripcion}</p>${detalles.length ? `<ol class="tutorial__subpasos">${detalles.map((detalle, indice) => `<li>${detalle}${enlaces.filter(([, , pasoEnlace]) => pasoEnlace === indice).map(([ruta, texto]) => `<div class="formulario__acciones"><a class="boton boton--secundario boton--pequeno" href="#${ruta}">${texto}</a></div>`).join("")}</li>`).join("")}</ol>` : ""}</div></li>`;

function tutorialAdministrativo() {
  return encabezado({
    titulo: "Guía para el Administrador",
    descripcion: "Un recorrido práctico para registrar y revisar solicitudes, y acompañar la admisión desde las tareas que tenés habilitadas.",
  }) + `<div class="rejilla">${tarjeta({
    titulo: "Antes de empezar",
    cuerpo: '<p>Seleccioná tu institución y el período de inscripción correspondiente. El Directivo prepara el período, configura la oferta y abre la recepción. <strong>Empezá a registrar solicitudes cuando la recepción figure como abierta.</strong></p><p>Si el período no está abierto o necesitás corregir una solicitud después del cierre, avisale al Directivo del jardín.</p>',
  })}${tarjeta({
    titulo: "Tu recorrido de trabajo",
    cuerpo: `<ol class="tutorial__pasos">${paso(
      1,
      "Registrá las solicitudes",
      "Cargá los datos de cada aspirante y la información que la familia brinda para la inscripción.",
      [["solicitudes", "Ir a Solicitudes", 0]],
      [
        "Confirmá que el jardín y el período seleccionados sean los correctos y que la recepción esté abierta. En <strong>Solicitudes</strong>, elegí <strong>Nueva solicitud</strong>.",
        "Ingresá nombre, apellido y fecha de nacimiento. El DNI es opcional: si no lo tenés, dejalo vacío; no completes un número estimado.",
        "Completá domicilio, turno preferido y alternativas aceptadas por la familia. Guardá y abrí <strong>Ver ficha</strong> para revisar lo registrado.",
        "Una solicitud registrada no confirma una vacante. La admisión se resuelve en etapas posteriores.",
      ],
    )}${paso(
      2,
      "Revisá y completá las fichas",
      "Corroborá los datos y registrá la documentación y las condiciones informadas.",
      [
        ["solicitudes", "Ir a Solicitudes", 0],
        ["documentacion", "Ir a Documentación", 1],
      ],
      [
        "Buscá la solicitud y abrí <strong>Ver ficha</strong>. Mientras la recepción esté abierta, podés corregir los datos del aspirante y de la inscripción.",
        "En <strong>Documentación y condiciones</strong>, anotá lo recibido y su estado de revisión. Una condición declarada no se verifica automáticamente ni otorga prioridad por sí sola.",
        "Si la recepción ya cerró, no intentes corregir desde otro lugar: solicitá al Directivo que evalúe el caso.",
      ],
    )}${paso(
      3,
      "Revisá domicilios y posibles coincidencias",
      "Ayudá a mantener los datos de ubicación claros y avisá cuando una decisión requiera permisos de Directivo.",
      [
        ["domicilios", "Ir a Domicilios y distancias", 0],
        ["duplicados", "Consultar posibles duplicados", 1],
      ],
      [
        "En <strong>Domicilios y distancias</strong>, revisá la dirección. Las distancias se ingresan manualmente y figuran como provisorias; una distancia pendiente no significa cero metros.",
        "En <strong>Posibles duplicados</strong>, compará las fichas. Tu rol puede consultar las alertas, pero la decisión de confirmar o descartar una coincidencia corresponde al Directivo.",
      ],
    )}${paso(
      4,
      "Colaborá con la preparación de admisiones",
      "Podés preparar información y propuestas; el Directivo conserva las confirmaciones finales.",
      [
        ["vacantes/prioridades", "Ver prioridades y sorteo", 0],
        ["vacantes", "Ir a Vacantes y admisión", 1],
      ],
      [
        "En <strong>Prioridades y sorteo</strong>, revisá los casos pendientes y registrá el sorteo cuando corresponda. Si el sistema señala una decisión que no tenés habilitada, coordiná con el Directivo.",
        "En <strong>Propuestas</strong>, podés generar la propuesta de ingresos para que sea revisada. La propuesta todavía no confirma ingresos: el Directivo debe revisarla y confirmarla.",
      ],
    )}${paso(
      5,
      "Atendé la lista de espera",
      "Cuando quede disponible una vacante, colaborá con el ofrecimiento y el seguimiento de la respuesta familiar.",
      [["vacantes/espera", "Ir a Lista de espera", 0], ["vacantes/ofrecimientos", "Ir a Ofrecimientos", 1]],
      [
        "Consultá la lista y respetá los candidatos compatibles que presenta el sistema.",
        "En <strong>Ofrecimientos</strong>, registrá la oferta de la vacante y la respuesta de la familia. Si la familia acepta, avisá al Directivo para que confirme el ingreso definitivo.",
      ],
    )}${paso(
      6,
      "Consultá resultados y distribución",
      "Revisá la información disponible y coordiná las confirmaciones que corresponden al Directivo.",
      [["reportes", "Ir a Resultados e historial", 0], ["secciones/distribuciones", "Consultar distribuciones", 1]],
      [
        "En <strong>Resultados e historial</strong>, consultá los totales y descargá los reportes habilitados para tu cuenta.",
        "Podés colaborar generando una propuesta de distribución. La definición de secciones y la confirmación de la distribución quedan a cargo del Directivo.",
      ],
    )}</ol>`,
  })}</div><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">¿Cuándo necesitás al Directivo?</h2></div><div class="tarjeta__cuerpo"><p>Pedile ayuda para abrir o cerrar la recepción, corregir datos después del cierre, resolver alertas de posibles duplicados, confirmar ingresos, definir secciones o confirmar una distribución. Si una pantalla muestra que una acción requiere Directivo, esa restricción es intencional.</p></div></section>`;
}

export function tutorial(ctx) {
  const esRoot = ctx.estado.usuario?.roles?.includes("AdministradorDelSistema") ?? false;
  const rolInstitucional = ctx.estado.usuario?.accesos?.find(
    (acceso) => acceso.institucionId === ctx.estado.institucion?.id,
  )?.rol;
  if (!esRoot && (rolInstitucional ?? ctx.estado.usuario?.roles?.[0]) === "Administrativo")
    return tutorialAdministrativo();
  const esDirectivo = !esRoot && (rolInstitucional ?? ctx.estado.usuario?.roles?.[0]) === "Directivo";
  return (
    encabezado({
      titulo: esRoot ? "Recorrido general del sistema" : "Guía para el Directivo",
      descripcion: esRoot
        ? "Recorrido completo del ciclo lectivo y administración global de la plataforma."
        : "Prepará el jardín y coordiná las etapas del ciclo que requieren permisos de Directivo.",
    }) +
    (esRoot
      ? tarjeta({
          titulo: "Administración global de Root",
          cuerpo: '<p>Además del recorrido del ciclo, Root administra las cuentas globales y crea cuentas institucionales asignándoles un jardín y el rol Directivo o Administrativo. También consulta las instituciones asociadas a cada identidad y revisa el historial de accesos.</p><p>Las cuentas inactivas se consultan desde el filtro de estado. El DNI aparece enmascarado y permite vincular cuentas de una misma persona.</p><a class="boton boton--secundario" href="#usuarios">Ir a Usuarios y permisos</a>',
        })
      : tarjeta({
          titulo: "Antes de abrir la inscripción",
          cuerpo: '<p>Revisá la configuración de tu jardín, el período, la oferta y la fecha de corte antes de abrir la recepción. También podés preparar los accesos del equipo desde <strong>Personal del jardín</strong>: las cuentas administrativas reciben una contraseña temporal y deben cambiarla en su primer ingreso.</p><a class="boton boton--secundario" href="#personal">Ir a Personal del jardín</a>',
        })) +
    `<div class="rejilla">${tarjeta({
      titulo: "Antes de comenzar",
      cuerpo:
        `<p>Iniciá sesión con una cuenta autorizada y seleccioná la institución y el período con el que vas a trabajar. ${esDirectivo ? "Las tareas de preparación y las confirmaciones finales están disponibles para tu rol." : "Las acciones disponibles dependen de tus permisos."}</p><p class="formulario__nota">La sesión se cierra al vencer el token o al recargar la página. Si volvés a entrar, seleccioná el contexto correcto antes de continuar.</p>`,
    })}${tarjeta({
      titulo: "Recorrido del ciclo lectivo",
      cuerpo: `<ol class="tutorial__pasos">${paso(
        1,
        "Prepará la institución",
        "Dejá completos los datos del jardín antes de configurar la inscripción.",
        [["institucion", "Mi institución", 1]],
        [
          "Seleccioná la institución correcta en el encabezado. Si tu cuenta todavía no tiene una, pedí que te asignen acceso o registrala si tenés permiso.",
          "En <strong>Mi institución</strong>, revisá el nombre, teléfono, correo y domicilio. Corregí los datos que estén desactualizados.",
          "La ubicación se usa como referencia para las distancias. En esta versión se carga manualmente; verificá que el domicilio del jardín esté bien escrito antes de continuar.",
        ],
      )}${paso(
        2,
        "Configurá el período y la oferta",
        "Prepará el ciclo lectivo, las vacantes disponibles y la fecha con la que se calcularán las edades.",
        [
          ["periodos", "Períodos", 0],
          ["oferta", "Aulas, turnos y oferta", 1],
          ["periodos", "Períodos", 2],
          ["periodos", "Períodos", 4],
        ],
        [
          "En <strong>Períodos</strong>, elegí <strong>Crear período</strong> e indicá el ciclo lectivo y sus fechas previstas. El período se crea en estado de preparación; todavía no recibe solicitudes.",
          "En <strong>Aulas, turnos y oferta</strong>, prepará las aulas y los turnos. Creá o reutilizá una combinación del catálogo y agregala al ciclo. En la oferta del ciclo ajustá la capacidad y dejala habilitada si se ofrecerá en esa inscripción.",
          "Volvé a <strong>Períodos</strong> y elegí <strong>Preparar configuración inicial</strong>. Ingresá la fecha de corte aprobada para ese ciclo: la aplicación publicará las reglas y la oferta actual. <strong>La fecha de corte indica el día exacto en que se calcula la edad para determinar la sala; no usa la edad que tiene el aspirante al momento de inscribirlo.</strong>",
          "Ejemplo: si para el ciclo 2027 la fecha de corte es <strong>30/06/2027</strong>, quien nació el 30/06/2024 tiene 3 años en esa fecha; quien nació el 01/07/2024 todavía tiene 2. Para las salas de 3 a 5 años, el rango de nacimiento de este ejemplo va del 01/07/2021 al 30/06/2024, inclusive. Fuera de ese rango la sala queda pendiente de revisión. La fecha no se completa sola: verificá el criterio del jardín antes de publicar.",
          "Después de preparar el ciclo, al guardar una oferta nueva o ajustada se publica automáticamente para ese período, sin volver a pedir la fecha de corte. Si la recepción está abierta, confirmá que el cambio se aplique a las nuevas solicitudes. <strong>Cambiar fecha de corte</strong> sigue siendo una acción aparte. En la fila del ciclo, elegí <strong>Abrir recepción</strong> cuando estés listo; la apertura es manual.",
        ],
      )}${paso(
        3,
        "Registrá las solicitudes",
        "Cada solicitud se completa en dos partes: primero los datos personales del aspirante y luego la información de inscripción.",
        [["solicitudes", "Solicitudes", 0]],
        [
          "Comprobá que la institución y el período correctos estén seleccionados y que la recepción esté abierta. En <strong>Solicitudes</strong>, elegí <strong>Nueva solicitud</strong>.",
          "En la primera parte, escribí nombre, apellido y fecha de nacimiento. El DNI es opcional: dejalo vacío si no lo tenés, no ingreses un número inventado. Si el aspirante ya existe, podés seleccionarlo con su identificador.",
          "En la segunda parte, completá el domicilio, elegí el turno preferido y marcá como alternativas solo los turnos que la familia aceptó. Luego guardá la solicitud.",
          "La solicitud registrada todavía no significa que el aspirante haya obtenido una vacante. Abrí su ficha para agregar responsables y continuar con las revisiones.",
          "Si la recepción está cerrada, la pantalla avisa que el registro quedará fuera de término y pendiente de autorización. No continúes por esa vía salvo que corresponda.",
        ],
      )}${paso(
        4,
        "Revisá la información",
        "Antes de cerrar la recepción, revisá que cada solicitud tenga la información necesaria y atendé las observaciones que aparezcan.",
        [
          ["solicitudes", "Fichas", 0],
          ["domicilios", "Domicilios y distancias", 2],
          ["duplicados", "Posibles duplicados", 3],
          ["vacantes/prioridades", "Prioridades y sorteo", 4],
        ],
        [
          "En <strong>Solicitudes</strong>, buscá por nombre, apellido o DNI y elegí <strong>Ver ficha</strong>. Desde la ficha también podés entrar en <strong>Documentación y condiciones</strong> para revisar los documentos y requisitos. Mientras la recepción esté abierta, podés corregir los datos del aspirante y de la inscripción. <strong>Si corregís la fecha de nacimiento, se recalculan la edad y la sala de las solicitudes de ese aspirante en el período usando la misma fecha de corte publicada para el ciclo.</strong>",
          "En <strong>Documentación y condiciones</strong>, registrá los antecedentes recibidos y su estado. Marcá las condiciones declaradas y su revisión según la información disponible; declarar una condición no la verifica automáticamente ni otorga prioridad por sí sola.",
          "En <strong>Domicilio y distancia</strong>, comprobá la dirección. Si cargás una distancia, ingresá la medición obtenida manualmente y cómo se obtuvo: se mostrará como provisoria. Una distancia pendiente no equivale a cero.",
          "En <strong>Posibles duplicados</strong>, revisá cada coincidencia. Registrá si corresponde a la misma persona o a personas diferentes y explicá el motivo. El sistema no fusiona ni elimina solicitudes automáticamente.",
          "Cuando las solicitudes estén revisadas, recién entonces cerrá la recepción y pasá al orden de prioridades del paso siguiente.",
        ],
      )}${paso(
        5,
        "Cerrá la recepción y revisá el orden de prioridad",
        "Cuando ya no vayas a recibir ni corregir solicitudes para este ciclo, cerrá la etapa de inscripción y atendé los controles previos a la admisión.",
        [
          ["periodos", "Períodos", 0],
          ["vacantes/prioridades", "Prioridades y sorteo", 1],
          ["vacantes/prioridades", "Prioridades y sorteo", 2],
          ["vacantes", "Propuestas", 3],
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
        [
          ["vacantes", "Propuestas", 0],
          ["vacantes", "Propuestas", 1],
          ["vacantes", "Propuestas", 2],
          ["vacantes", "Propuestas", 3],
        ],
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
          ["secciones", "Secciones", 0],
          ["secciones", "Secciones", 1],
          ["secciones", "Secciones", 2],
          ["secciones/distribuciones", "Distribuciones", 3],
          ["secciones/distribuciones", "Distribuciones", 4],
          ["secciones/distribuciones", "Distribuciones", 5],
        ],
        [
          "Hacé este paso después de confirmar los ingresos. En la pestaña <strong>Secciones</strong>, elegí <strong>Definir sección</strong>. Completá un nombre, seleccioná una <strong>oferta habilitada</strong> (aula y turno) y elegí la composición por edades. La capacidad se toma de la oferta publicada; si no aparece ninguna, primero hay que habilitar y publicar una oferta en <strong>Aulas, turnos y oferta</strong>.",
          "Una sección <strong>pura</strong> recibe una sola edad (3, 4 o 5). Una <strong>multiedad</strong> puede recibir una combinación habilitada, como 3 y 4 o 4 y 5. Usá multiedad solo si esa combinación corresponde a la organización aprobada para el jardín. Si necesitás convertir una sección pura, usá <strong>Ampliar a multiedad</strong>; el sistema comprobará compatibilidad y capacidad.",
          "Antes de generar la distribución, verificá que exista una sección para cada combinación de aula, turno y edad que necesiten los ingresos. Si falta una opción o no alcanza la capacidad, agregá o ajustá las secciones antes de continuar.",
          "Abrí <strong>Distribuciones</strong> y elegí <strong>Generar distribución</strong>. El sistema propone una sección compatible para cada ingreso activo que todavía no tenga asignación. Crear la propuesta no guarda asignaciones definitivas.",
          "Abrí la versión recién creada y revisá cada fila: edad, turno y sección propuesta. Si hay una sección incorrecta, elegí <strong>Revisar asignación</strong> y seleccioná una de las secciones compatibles. Si ninguna sirve, dejá el ingreso sin sección e indicá el motivo; después corregí la configuración y generá una nueva distribución.",
          "Cuando hayas revisado todas las filas, seleccioná <strong>Confirmar distribución</strong> y marcá la confirmación de revisión. Solo entonces se guardan las asignaciones definitivas. Si cambian los ingresos o las secciones, la propuesta queda desactualizada y no se puede confirmar.",
        ],
      )}${paso(
        8,
        "Atendé la lista de espera y consultá los resultados",
        "Si se libera una vacante después de las admisiones, podés ofrecerla a una persona compatible de la lista de espera. Después consultá el historial del ciclo.",
        [
          ["vacantes/espera", "Lista de espera", 0],
          ["vacantes/ingresos", "Ingresos y bajas", 1],
          ["vacantes/ofrecimientos", "Ofrecimientos", 2],
          ["vacantes/ofrecimientos", "Ofrecimientos", 3],
          ["vacantes/ofrecimientos", "Ofrecimientos", 4],
          ["vacantes/ofrecimientos", "Ofrecimientos", 5],
          ["reportes", "Resultados e historial", 6],
        ],
        [
          "La <strong>Lista de espera</strong> muestra a quienes quedaron esperando en el resultado confirmado, sus posiciones y los casos pendientes de revisión. Revisá los avisos: si el sistema marca datos u orden pendientes, resolvé esa revisión antes de ofrecer una plaza. No cambies el orden eligiendo a alguien que no figure entre los candidatos compatibles.",
          "Si una familia informa que deja un ingreso, abrí <strong>Ingresos y bajas</strong> y elegí <strong>Registrar baja</strong>. Escribí el motivo y guardá. Si la baja queda pendiente de coordinación, el lugar todavía se considera ocupado; desde la misma fila podés consultar el estado y reintentarla cuando corresponda.",
          "Cuando la vacante esté disponible, entrá en <strong>Ofrecimientos</strong> y elegí <strong>Consultar candidatos</strong>. Seleccioná el aula y turno de la vacante; el sistema filtra a las personas que pueden ocupar ese lugar según su edad, turno, posición y disponibilidad.",
          "Revisá las posiciones de la lista devuelta. En la fila de la persona elegida, seleccioná <strong>Ofrecer plaza</strong> e indicá el motivo. El ofrecimiento crea una reserva temporal para evitar que esa vacante se use dos veces; todavía no crea un ingreso definitivo.",
          "Cuando la familia responda, usá <strong>Registrar respuesta</strong>. Elegí Aceptado, Rechazado o Cancelado e indicá el motivo. Si acepta, cargá también la constancia familiar: la reserva se mantiene mientras se confirma el ingreso. Si rechaza o cancela, la reserva se libera y la persona no se elimina de la lista de espera.",
          "Para finalizar una aceptación, elegí <strong>Confirmar ingreso</strong> en el ofrecimiento aceptado. Esa confirmación crea el ingreso definitivo. Si la familia rechaza un turno alternativo, conserva su lugar en la espera para las opciones que todavía acepta.",
          "Al terminar, abrí <strong>Resultados e historial</strong>. Revisá el resumen del período, ingresos y bajas, cambios y distribuciones. Usá <strong>Descargar reporte CSV</strong> o <strong>Descargar nómina CSV</strong> para exportar la información disponible para tu cuenta.",
        ],
      )}</ol>`,
    })}</div><section class="tarjeta tarjeta--separada"><div class="tarjeta__cabecera"><h2 class="tarjeta__titulo">Si algo no coincide</h2></div><div class="tarjeta__cuerpo"><p>Si el sistema informa que los datos cambiaron o que una operación está desactualizada, cerrá el formulario, consultá nuevamente la ficha y verificá los datos antes de volver a guardar. Las distancias se cargan manualmente y se marcan como provisorias; una distancia pendiente no equivale a cero.</p><p class="formulario__nota">Algunas decisiones, como confirmar ingresos o distribuciones, requieren permisos de Directivo. Si no ves la acción, consultá con una persona que tenga ese permiso.</p><a class="boton boton--texto" href="#ayuda">Ver ayuda y límites de esta versión</a></div></section>`
  );
}
