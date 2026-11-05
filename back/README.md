# Gestión de inscripciones de jardines

Backend C#/.NET 10 organizado como monolito modular. El frontend vive en ../front, utiliza HTML, CSS y JavaScript y tiene su propia documentación.

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

Usuarios dispone de cuentas, Identity y JWT. Configuración incorpora instituciones, períodos, oferta y versiones. Inscripciones implementa personas, solicitudes, antecedentes, correcciones, duplicados y revisión/carga manual de domicilios y distancias. Vacantes incorpora prioridades, sorteo, casos de revisión, propuestas y confirmación de ingresos, espera, bajas, ofrecimientos y nómina distrital. Secciones permite definir grupos, revisar/confirmar distribuciones y ampliar una sección pura a multiedad. Reportes reúne resultados e historial. El frontend integra estos recorridos en su primera versión.

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

## HU-13 — Cierre de recepción

El cierre de Configuración ya integrado con Inscripciones impide nuevas recepciones ordinarias y conserva las posteriores como fuera de término pendientes de autorización. Permite continuar revisiones y conserva las versiones asignadas e históricas. Se agregó una prueba de integración específica de estado, versiones, historial y permisos.

Quedan pendientes el procedimiento de autorización fuera de término y los procesos reales de propuestas/confirmación. Esta revisión no agrega tablas, migraciones ni otro endpoint de cierre.

## HU-12 — Revisión manual de domicilios

Inscripciones permite registrar domicilios pendientes, observados o revisados manualmente con motivo, referencia, usuario, fecha e historial. Cada decisión conserva la dirección y su revisión; corregirla deja la versión nueva pendiente, mientras que cambiar otros datos conserva la comprobación actual.

Reutiliza la corrección de HU-09 y la carga de distancia manual de HU-11. La revisión puede continuar tras el cierre con permisos, control de concurrencia y migración propia. Localización, reintentos y cálculo automático con Geoapify siguen pendientes; revisar no certifica coordenadas ni genera distancias.

## HU-18 — Primera versión de propuestas

Vacantes incorpora generación, consulta e historial de propuestas con distribución provisional factible por edad, turno y capacidad; distingue ingreso propuesto, espera y pendientes. Respeta prioridades antes de aplicar sorteo y utiliza resoluciones manuales suficientes y vigentes. La comparación de cercanía utiliza la precisión publicada, actualmente dos decimales, conservando el valor original.

Incluye API con permiso institucional, instantáneas protegidas, vigencia informativa y registro para descontar ocupaciones activas. Generar no confirma ingresos ni reserva plazas. HU-19/HU-21 deben mantener ese registro en sus operaciones reales; las páginas de Vacantes del frontend todavía no lo consumen.

Vacantes mantiene scripts SQL manuales numerados: aplicar `src/Modulos/Vacantes/Infrastructure/Persistencia/Migraciones/004_propuestas_ingresos.sql` después de los tres anteriores antes de usar estas operaciones en PostgreSQL. No se aplicó el script sobre la base local durante la implementación.

El detalle y los pendientes están en `docs/HU-18-Primera-version.md` y `docs/Cosas pendientes.md`.

## HU-20 — Consulta de lista de espera

Vacantes permite consultar la espera por institución y período a partir de una propuesta confirmada de HU-19. Separa aspirantes elegibles de casos pendientes, excluye ingresos existentes y conserva preferencias, condiciones, distancia y antecedentes del sorteo/resoluciones. Los filtros por grupo etario, turno o estado y la paginación mantienen una misma lista y su orden general.

Operación: `GET /api/v1/instituciones/{institucionId}/periodos/{periodoId}/lista-espera`. Requiere el permiso institucional `Admision.ConsultarListaEspera`. No confirma ingresos ni gestiona ofrecimientos. No agrega tablas ni migraciones; necesita los scripts previos de Vacantes, incluido 005 de HU-19.

El detalle, los resultados de prueba y las limitaciones están en `docs/HU-20-Primera-version.md` y `docs/Cosas pendientes.md`. La página del frontend continúa pendiente de integración.

## HU-21 — Bajas y ofrecimientos desde espera

Vacantes incorpora bajas con motivo/autor/fecha y coordinación con Secciones; ofrecimientos con reservas activas; aceptación, rechazo y cancelación; y confirmación manual revalidada. Un rechazo de turno alternativo no retira al aspirante de la espera. Las operaciones repetidas no duplican reservas ni ingresos.

La baja que no logra completar la coordinación devuelve 202 y conserva ocupada la plaza hasta el reintento autorizado. Secciones conserva el historial de su asignación y utiliza oferta/reglas publicadas de Configuración.

Antes de utilizarlo en PostgreSQL, aplicar los scripts `src/Modulos/Vacantes/Infrastructure/Persistencia/Migraciones/006_bajas_ofrecimientos.sql` y `src/Modulos/Secciones/Infrastructure/Persistencia/Migrations/002_bajas_ingresos.sql` sobre los esquemas previos. No se ejecutaron sobre la base local durante este trabajo.

Detalle, operaciones y pendientes: `docs/HU-21-Primera-version.md`. La integración con el frontend y las pruebas específicas con PostgreSQL siguen pendientes.


### HU-26: cambio de sección pura a multiedad

El backend permite ampliar edades conservando la edad original, con permiso institucional, revisión vigente, capacidad libre y compatibilidad de alumnos y reservas. Guarda historial y marca borradores de distribución para revisión; los ingresos posteriores siguen el ofrecimiento de HU-21.

- POST `/api/v1/periodos/{periodoId}/secciones/{seccionId}/composicion`: `nuevasEdades`, `motivo` y `revision`.
- GET `/api/v1/periodos/{periodoId}/secciones/{seccionId}/composicion/historial`.
- Script adicional de Secciones: `src/Modulos/Secciones/Infrastructure/Persistencia/Migrations/003_cambios_composicion.sql`. Su aplicación es manual y requiere validar previamente la base y los scripts anteriores.


## Comprobación de la integración frontend/backend

`dotnet run --project pruebas/Jardin.Configuracion.Pruebas -- --servidor-integracion` inicia temporalmente la API en http://127.0.0.1:5189 con Identity/JWT y bases SQLite aisladas. Desde la raíz del repositorio, `node front/pruebas/integracion-real.mjs` recorre las operaciones conectadas. Finalizar con Ctrl+C; no usa ni prepara la base real. No ejecutar compilaciones simultáneas mientras ese ejecutable está abierto.

Para el entorno local real, copiar `.env.example` como `.env`, completar conexión/JWT y preparar las migraciones y scripts de todos los módulos. La primera versión conserva el cálculo manual de distancia. No se considera que los scripts PostgreSQL estén validados por pasar las pruebas SQLite.


### Desarrollo local con secretos de usuario (opción preferida)

En Visual Studio, sobre Jardin.Api, abrir «Administrar secretos de usuario». El proyecto tiene UserSecretsId y WebApplication.CreateBuilder los carga en Development. Guardar allí ConnectionStrings:DefaultConnection, Jwt:ClaveSecreta y, para crear Root por primera vez, UsuarioRoot:Clave. Mantener appsettings.json con la configuración general. No duplicar estas claves en .env ni compartir secrets.json. La conexión real y los esquemas requieren comprobación separada.
