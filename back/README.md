# Gestión de inscripciones de jardines

Backend C#/.NET 10 organizado como monolito modular. El frontend vive en ../front y tendrá su propio stack y documentación.

## Estructura

Desde esta carpeta back, Jardin.sln agrupa la API, los veinte proyectos de los cinco módulos y el ejecutable de comprobaciones de Configuración.

```text
back/
  Jardin.sln
  src/
    Jardin.Api/
    Modulos/
      Usuarios/
      Configuracion/
      Inscripciones/
      Vacantes/
      Secciones/
  pruebas/
    Jardin.Configuracion.Pruebas/
  docs/
```

Cada módulo tiene Domain, Application, Infrastructure y Contracts.

- **Domain:** conceptos y reglas del negocio.
- **Application:** coordinación de casos de uso.
- **Infrastructure:** persistencia y servicios externos.
- **Contracts:** operaciones y datos públicos del módulo.
- **API:** peticiones y respuestas HTTP.

## Estado actual

Usuarios dispone del trabajo de cuentas, Identity y JWT desarrollado por el compañero. Configuración incorpora registro, consulta, actualización, permisos, historial y migración propia. Inscripciones conserva sus clases iniciales de dominio; los procesos de mediciones y propuestas aún están pendientes.

La detección de referencias afectadas por cambios de ubicación está preparada, pero la integración con mediciones y confirmaciones reales no puede declararse terminada hasta construir sus procesos de origen.

El estado detallado y los comandos de uso están en [HU-01 — Implementación](docs/HU-01-Implementacion.md). Los documentos de continuidad anteriores conservan el contexto histórico de cada fecha.

## Compilar y comprobar

```powershell
dotnet build Jardin.sln
dotnet run --project pruebas/Jardin.Configuracion.Pruebas
```

Se necesita el SDK de .NET 10. La suite utiliza SQLite aislado por defecto. El documento de HU-01 explica cómo ejecutar también las comprobaciones con PostgreSQL exclusivo de pruebas.

## Configuración local

La API requiere Jwt__ClaveSecreta y la conexión de Usuarios existente. Configuración admite ConnectionStrings__Configuracion y, si se omite, utiliza DefaultConnection de la API. No subir contraseñas, claves JWT ni tokens al repositorio.

Antes de arrancar contra una base local, aplicar la migración de Configuración siguiendo el documento de HU-01. Las pruebas no requieren modificar las bases de la aplicación.

Geoapify sigue siendo el proveedor aprobado de coordenadas y distancia para la integración posterior. No se implementó Haversine.

## Primera versión de HU-02

Configuración también permite crear y consultar períodos y abrir/cerrar recepción manualmente. Se pospuso por decisión del usuario la validación de oferta/reglas al abrir.

- [HU-02 — Primera versión](docs/HU-02-Primera-version.md).
- [Cosas pendientes de las historias trabajadas](<docs/Cosas pendientes.md>).

## Primera versión de HU-03

Configuración permite registrar aulas y turnos, configurar oferta por período, disponibilidad, capacidad documentada y ocupaciones iniciales por edad. Incluye permisos, historial y revisión concurrente.

- [HU-03 — Operaciones, ejemplos, migración y comprobaciones](docs/HU-03-Primera-version.md).
- La capacidad neta inicial no es el cálculo final de vacantes. Las integraciones y decisiones pendientes se mantienen en Cosas pendientes.md.

## Primera versión de HU-04

Configuración permite publicar versiones inmutables de reglas y oferta y elegir explícitamente cuáles utiliza un período. Las publicaciones anteriores permanecen consultables; cambiar el borrador de oferta no modifica esas copias. Las consultas de períodos incluyen VersionReglasId y VersionOfertaId.

Se publican las reglas acordadas del perfil Prototipo-2026-1 con fecha de corte explícita. La comprobación de vigencia compara las referencias actuales; su integración con propuestas, resultados y confirmación transaccional sigue pendiente. La apertura sin configuración válida de HU-02 conserva la omisión aprobada para el prototipo.

## HU-06 — Personas de Inscripciones

Inscripciones permite registrar y consultar aspirantes y responsables, buscar responsables por institución y vincular una misma persona con varios aspirantes. Incluye registro sin DNI, permisos actuales por institución, auditoría y migración propia en el esquema inscripciones. La solicitud asociada a período/domicilio todavía corresponde a HU-07.

La conexión admite ConnectionStrings__Inscripciones y, si se omite, utiliza DefaultConnection. Aplicar las migraciones de ContextoInscripciones antes de usar estos endpoints en la base local.

## HU-07 — Solicitudes de inscripción

Inscripciones permite registrar, consultar y listar solicitudes con domicilio, sala calculada a la fecha de corte publicada, preferencias de turno y versiones utilizadas. Conserva datos históricos e identifica solicitudes fuera de término pendientes de autorización y edades que requieren revisión.

El registro requiere versiones de reglas/oferta asignadas al período. Configuración protege su estado con un bloqueo de fila en PostgreSQL durante el registro para coordinar cierres o reasignaciones simultáneos. La autorización fuera de término, las excepciones de edad y el tratamiento de prioridades/mediciones corresponden a etapas posteriores.

## HU-08 — Documentación y condiciones declaradas

Inscripciones permite registrar documentación pendiente/presentada/observada y condiciones declaradas con revisión separada. Conserva la solicitud ante faltantes, sin exigir archivos digitalizados. Incluye permisos específicos por institución, historial anterior/nuevo, actualización de la revisión de solicitud y protección contra ediciones simultáneas.

La revisión puede continuar después del cierre; incorporar una declaración nueva requiere el permiso especial de corrección. El catálogo de documentos obligatorios y la evaluación automática de prioridades pertenecen a definiciones/procesos posteriores.

## Primera versión de HU-09 — Búsqueda y correcciones

Inscripciones permite buscar solicitudes por identificador o datos del aspirante y corregir dirección, preferencias, consentimiento y condiciones declaradas. Las correcciones conservan historial, requieren la revisión actual y un permiso especial después del cierre; cambiar una declaración reinicia su verificación.

Se conserva la oferta histórica y se distingue RevisionDatos de RevisionDomicilio. El índice de referencias permite consultar usos registrados desactualizados; su integración con mediciones/propuestas reales y bloqueo de confirmación sigue pendiente. La corrección de la persona compartida requiere una operación específica acordada posteriormente.

## Primera versión de HU-10 — Posibles duplicados

Inscripciones genera alertas al registrar solicitudes y permite detectar coincidencias existentes dentro del mismo jardín y período. Los criterios son mismo aspirante, DNI completo coincidente o nombre/apellido/nacimiento iguales, sin comparación aproximada.

Directivo y Root pueden registrar duplicación confirmada o personas diferentes con motivo, usuario, fecha e historial. Incluye consulta paginada, permisos institucionales, migración y protección ante revisiones simultáneas. Repetir una detección sin datos nuevos conserva las decisiones tomadas.

Ambas solicitudes se conservan. Elegir cuál continúa y aplicar esa decisión a procesos posteriores queda pendiente del procedimiento acordado; confirmar una alerta no excluye automáticamente una solicitud.

## HU-11 — Primera versión con distancia manual

Inscripciones permite ingresar provisionalmente una distancia en metros, con decimales, referencia de cómo se obtuvo, usuario y fecha. Corregir agrega una nueva carga y conserva el historial. Las consultas distinguen el último registro y señalan si cambiaron los domicilios utilizados; incluye permisos, migración y protección de concurrencia.

Por decisión del usuario, Geoapify, la localización y el cálculo automático quedan pospuestos. El origen se identifica como ManualProvisional; esta alternativa no completa los criterios automáticos de HU-11 ni verifica la precisión física del dato.
