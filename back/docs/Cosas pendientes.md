# Cosas pendientes

Registro de lo que falta en las historias que ya trabajamos. Fecha de actualización: 2 de noviembre de 2026.

Este archivo permite construir una primera versión funcional sin perder de vista los criterios aún no implementados. No reemplaza el backlog completo ni significa que las historias estén cerradas.

## Cómo mantener este registro

- Anotar la historia, qué falta, por qué se pospone y qué debe comprobarse al resolverlo.
- Distinguir lo pospuesto por decisión del usuario de lo que depende de otros módulos.
- Cuando se resuelva un punto, registrar la fecha y su comprobación; no darlo por resuelto solo por compilar.
- Mantener las tareas de Trello coherentes con este estado cuando el usuario autorice actualizarlas.

## Pendientes de HU-01 — Datos del jardín

| Código | Qué falta | Motivo | Cómo se comprobará |
|---|---|---|---|
| HU01-01 | Registrar las referencias reales de cada medición de distancia. | Inscripciones todavía no implementa el proceso que genera esas mediciones. Configuración dispone del contrato y del índice. | Crear una medición desde su proceso real, cambiar la ubicación del jardín y comprobar que se identifica como afectada sin sobrescribir la original. |
| HU01-02 | Registrar las entradas reales de propuestas de ingreso y distribución y distinguir cuáles están vigentes. | Vacantes y Secciones todavía no tienen esos procesos. El índice actual no conoce sus estados del negocio. | Generar propuestas reales y consultar el impacto de un cambio de ubicación. |
| HU01-03 | Impedir confirmar una propuesta afectada y coordinar la comprobación con la confirmación. | Falta el proceso real de confirmación y su coordinación entre módulos. Consultar la revisión antes de confirmar no basta si puede cambiar entre ambos pasos. | Intentar confirmar con entradas obsoletas y con un cambio concurrente; ambas situaciones deben tratarse sin confirmar datos inconsistentes. |

**Ya funcional:** registrar, consultar, actualizar, permisos, persistencia, historial y consulta de referencias registradas. No marcar HU-01 completamente terminada mientras queden los puntos anteriores.

## Pendientes de HU-02 — Período de inscripción

| Código | Qué falta | Motivo | Cómo se comprobará |
|---|---|---|---|
| HU02-01 | Impedir la apertura con oferta o configuración incompletas/inválidas. | **Pospuesto expresamente por el usuario para la primera versión.** Hoy Abrir valida el estado y las fechas, pero no consulta aulas, turnos, capacidades o reglas. | Con HU-03/HU-04 disponibles, intentar abrir con cada dato requerido ausente o inválido y verificar el rechazo con una explicación. |
| HU02-02 | Asociar las versiones reales de reglas y oferta utilizadas por el período (RF-08). | Esas versiones se desarrollan con HU-03/HU-04. No se inventaron identificadores ni un indicador ficticio de configuración válida. | Consultar las versiones asociadas; cambiar una configuración futura y comprobar que el período conserva las referencias previstas. |

**Ya funcional:** crear el período asociado a institución/ciclo, consultar y listar sus fechas/estado, apertura/cierre manual, permisos, historial y revisión concurrente.

La primera versión permite abrir sin reglas ni oferta preparadas. Es una omisión temporal acordada, no una garantía de que luego se puedan procesar solicitudes o ingresos correctamente.

## Integración de HU-13 — Cierre de recepción

| Código | Qué falta | Motivo | Cómo se comprobará |
|---|---|---|---|
| HU13-01 | Que Inscripciones impida nuevas solicitudes ordinarias cuando la recepción está cerrada. | Por ahora Configuración solo guarda el estado del período. Inscripciones debe consultarlo dentro del registro real de solicitudes. | Cerrar la recepción y comprobar que el endpoint real de solicitudes rechaza una solicitud ordinaria. |
| HU13-02 | Tratamiento autorizado de solicitudes fuera de término. | Pendiente de definir el procedimiento P-11 e implementarlo con Inscripciones. | Comprobar quién puede recibirlas, su identificación y cómo se procesan sin desplazar automáticamente ingresos confirmados. |

Cerrar la recepción no finaliza la admisión ni debe bloquear revisiones, propuestas o distribución.

## Decisiones aún no definidas para períodos

No se agregaron restricciones sin requisitos confirmados. Antes de ampliar el comportamiento, acordar:

- Si se permite más de un período de la misma institución y ciclo.
- Si se permite reabrir una recepción cerrada y quién lo autoriza.
- Qué condiciones permiten finalizar el proceso completo.
- Si la apertura manual debe restringirse por el día actual y qué procedimiento permite excepciones.
- Cómo pueden corregirse las fechas o el ciclo de un período ya creado.

Hoy se aceptan fechas de inicio y fin informadas, con fin igual o posterior al inicio. El ciclo es un año entre 1 y 9999; no se exige que coincida con el año de las fechas, porque la recepción puede preparar el ciclo siguiente.

La apertura y el cierre son manuales. No hay un proceso automático que abra/cierre por calendario. No se implementaron reapertura, finalización ni edición de fechas/ciclo.

## Preparación local

Las migraciones se comprobaron en PostgreSQL aislado. El usuario debe revisar su conexión y aplicar las migraciones de Configuración en su base de desarrollo antes de probar manualmente contra ella.

No se aplicaron cambios a sus bases existentes ni se ejecutaron comandos de Git. La creación de ramas, el commit y la PR quedan a cargo del usuario.

## Pendientes de HU-03 — Aulas, turnos y oferta

**Ya funcional:** registro y consulta de aulas/turnos, oferta por período/aula/turno, capacidad y disponibilidad explícitas, ocupaciones iniciales por edad, referencia y origen, historial, permisos y concurrencia. Ver [HU-03 — Primera versión](HU-03-Primera-version.md).

| Código | Qué falta | Motivo | Cómo se comprobará |
|---|---|---|---|
| HU03-01 | Conciliar ocupaciones iniciales cuando se representen como ingresos confirmados. | El proceso de ingresos no está implementado y falta concretar P-18. No contar dos veces a la misma persona. | Registrar un ingreso que ya estaba incluido en el relevamiento y verificar que se descuenta una sola vez. |
| HU03-02 | Relacionar límites físicos del aula, por sección y por turno, y cantidad de secciones simultáneas. | Falta precisar P-19 y construir la comprobación real de factibilidad. | Evaluar propuestas reales con límites conjuntos; no sumar capacidades independientes ni aplicar automáticamente 25 como capacidad de aula. |
| HU03-03 | Asociar versiones de oferta al período y detectar propuestas afectadas por cambios. | Se completa con HU-04 y los procesos reales de propuestas. Revision controla ediciones concurrentes; el historial no sustituye una versión asociada al período. | Cambiar la oferta y comprobar qué versión utiliza el período y qué propuestas deben revisarse. |
| HU03-04 | Definir y aplicar el procedimiento de cambios con ingresos ya confirmados. | P-12 y los procesos de ingresos aún no permiten implementar ese control completo. | Intentar reducir capacidad o retirar disponibilidad con ingresos confirmados y comprobar el procedimiento acordado sin perderlos. |

CapacidadNetaInicial solo resta las ocupaciones declaradas cuando la oferta está disponible. No incluye reservas ni ingresos posteriores y no garantiza una distribución factible. No debe usarse como autorización automática de nuevos ingresos.

La primera versión permite actualizar la oferta sin consultar ingresos o propuestas reales. Estos puntos siguen pendientes; HU-03 no se declara completamente cerrada. Crear/editar oferta tampoco activa la validación de apertura pospuesta en HU-02.
