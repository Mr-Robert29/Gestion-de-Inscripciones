# Integración de la primera versión — 5 de noviembre de 2026

Se mantiene HTML, CSS BEM y JavaScript por módulos, sin framework ni dependencias de ejecución. La nomenclatura y los componentes existentes se reutilizan.

## Implementado

Se conectaron Vacantes, Secciones, Reportes y Usuarios con sus contratos HTTP. Incluye prioridades, sorteo y orden, resolución/reapertura de casos, generación/revisión/confirmación de propuestas, aceptación de turnos alternativos, espera, bajas, ofrecimientos y reservas, respuestas familiares, ingreso desde espera, definición de secciones, cambio a multiedad e historial, generación/revisión/confirmación de distribuciones y exportaciones CSV.

Los formularios guardan mediante una confirmación explícita. Mantienen revisiones y referencias requeridas; los reintentos de ofrecimientos reutilizan su operación. Cambiar contexto invalida formularios anteriores. La baja pendiente se informa como tal: no se anuncia la plaza libre hasta que el servidor confirma la coordinación.

Los textos externos se escapan. JWT permanece en memoria y las descargas utilizan la misma petición autenticada; no se agrega el token a enlaces. Un 401 finaliza la sesión y un 403 no se presenta como una consulta vacía.

## Correcciones del backend encontradas al integrar

- La consulta de historial de HU-26 ahora usa el permiso de lectura incorporado por HU-25; se actualizó la expectativa antigua de la prueba, conservando el rechazo al cambio sin permiso.
- Identificación del actor de distribución compatible con las claims de JWT de ASP.NET.
- Acceso institucional en operaciones de Secciones y distribuciones; listado HTTP de sus propuestas. Los errores se traducen a 400/403/404/409.
- Nueva distribución conserva las asignaciones activas y descuenta su ocupación; propone solo los ingresos sin sección.
- Consulta de historial de casos compatible con el ordenamiento DateTimeOffset en SQLite.
- La base de pruebas usa una conexión por contexto a memoria compartida aislada; evita compartir una conexión SQLite simultáneamente entre solicitudes.

## Evidencia y límites

Pruebas unitarias del frontend: 12 aprobadas. Recorrido `integracion-real.mjs`: 17 consultas de pantallas más escrituras y exportaciones, mediante API real e Identity/JWT, con SQLite descartable. No es un servidor simulado de respuestas.

Navegador: login, menú, cambio de institución, sección multiedad persistida, historial, distribuciones y generación desde un formulario; verificación móvil de 389 px sin desbordamiento de página y sin errores de consola observados.

El resultado final de las comprobaciones del backend y el estado de PostgreSQL se mantienen en el registro de continuidad local. La configuración inicialmente disponible no tenía JWT válido ni permitía conectar a PostgreSQL; el usuario indicó que completaría back/.env. No se ejecutaron migraciones sobre su base durante estas pruebas.

Consultar el README principal del frontend para el mapa de 16 pantallas y el recorrido operativo. Los límites pendientes de Geoapify y reglas de negocio no se consideran completados por disponer de un formulario.


## Arranque local con PostgreSQL comprobado

El 5 de noviembre se vinculó la API con los secretos configurados, se instaló la estructura en la base vacía y se comprobó login Root (200), mi-acceso (200), instituciones (200, sin registros), usuarios (200, una cuenta), CORS (204) y frontend (200). No se cargaron solicitudes ficticias en la base real. La prueba de todo el recorrido de admisión permanece realizada en SQLite aislado; no implica una validación de concurrencia multiinstancia en PostgreSQL.
