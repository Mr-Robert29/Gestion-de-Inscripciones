# HU-03 — Primera versión de aulas, turnos y oferta

Fecha: 2 de noviembre de 2026. Módulo: Configuración.

## Qué permite hacer

Registrar aulas y turnos del jardín y configurar cómo se utiliza cada combinación durante un período de inscripción: disponibilidad, capacidad documentada y ocupaciones iniciales por sala de edad. Permite consultar y actualizar esa oferta con permisos, historial y control de ediciones concurrentes.

Un aula es un espacio físico. Un turno indica cuándo se usa. La oferta relaciona ambos con un período. Por ejemplo, Aula 1 puede ofrecer 20 lugares físicos por la mañana y 20 por la tarde: son dos combinaciones diferentes. Esto no crea secciones ni confirma aspirantes.

## Dónde está cada responsabilidad

| Capa | Archivos principales | Para qué sirven |
|---|---|---|
| Domain | Entidades/Aula.cs y Turno.cs | Identifican los recursos de una institución; exigen un nombre. |
| Domain | Entidades/OfertaAulaTurno.cs y OcupacionInicial.cs | Representan capacidad, disponibilidad y ocupaciones; validan cantidades y conservan la revisión. |
| Contracts | IModuloOferta.cs y DTO Peticion/Respuesta/DatosOcupacionInicial | Definen lo que se puede pedir al módulo y los datos que entran o salen. |
| Application | Servicios/ServicioOferta.cs | Autoriza, comprueba las relaciones entre institución y período, transforma DTO y coordina el guardado. |
| Application | Persistencia/IRepositorioOferta.cs | Declara las consultas y escrituras que necesita Application. |
| Infrastructure | Persistencia/RepositorioOferta.cs y MapeoOferta.cs | Implementan esas operaciones con EF Core, PostgreSQL y guardado conjunto del historial. |
| API | Controllers/OfertaController.cs | Recibe HTTP y convierte resultados o errores en respuestas. |

IModuloOferta es otro contrato de Configuración; no es un sexto módulo. Infrastructure guarda los objetos que Application creó utilizando Domain.

## Datos y reglas implementadas

- Aulas y turnos pertenecen a una institución existente. Se identifican por Id; no se impuso unicidad por nombre.
- Cada oferta pertenece a un período existente. Su aula y turno deben pertenecer a la misma institución del período.
- Solo existe una oferta por período, aula y turno. El índice único también protege altas simultáneas.
- Capacidad es un entero no negativo informado expresamente. No se presume una capacidad predeterminada.
- Disponible debe informarse expresamente como true o false.
- ReferenciaCapacidad identifica el acta, relevamiento u otro respaldo del valor informado. Es un texto obligatorio; no se implementó carga de archivos.
- OrigenOcupaciones explica de dónde provino el relevamiento inicial y es obligatorio.
- Ocupaciones debe enviarse como lista, incluso si está vacía. [] significa que se confirmó que no hay ocupaciones iniciales; omitir el campo no significa cero.
- Cada ocupación indica Sala (3, 4 o 5 años) y Cantidad no negativa. No se repite una sala en la lista y la suma no puede superar la capacidad.
- Las salas aquí son grupos de edad del relevamiento; no son secciones ya creadas.
- Crear o modificar una oferta exige Motivo. Se conservan usuario, fecha, motivo y valores anteriores/nuevos en el historial institucional.
- Revision comienza en 1 y aumenta con cada actualización. El cliente debe enviar la revisión consultada. Una revisión anterior devuelve 409.
- Cambiar una oferta mantiene su Id, período, aula y turno. No se implementaron eliminación ni renombrado de recursos.

La referencia de 25 por sección no se aplica como límite automático del aula. La capacidad física informada no demuestra que una distribución de secciones sea factible.

## Qué significa CapacidadNetaInicial

Si está disponible, es Capacidad menos la suma de ocupaciones iniciales. Si no está disponible, devuelve cero.

Ejemplo: capacidad 20 y ocupaciones iniciales de 4 para sala de 3 y 2 para sala de 4 dan 14 de capacidad neta inicial. No significa que puedan confirmarse automáticamente 14 ingresos: todavía deben considerarse reglas, distribución, ingresos y reservas en sus procesos reales.

No se creó un algoritmo de distribución ni se definieron secciones puras/múltiples en esta historia.

## Operaciones de la API

Todas requieren una cuenta activa. Directivo con permiso de configuración institucional o Root puede escribir. Administrativo con acceso institucional puede consultar; se utiliza la misma política de HU-01/HU-02.

| Método | Ruta | Operación |
|---|---|---|
| POST / GET | /api/v1/instituciones/{institucionId}/aulas | Registrar / listar aulas. |
| POST / GET | /api/v1/instituciones/{institucionId}/turnos | Registrar / listar turnos. |
| POST / GET | /api/v1/periodos/{periodoId}/ofertas | Registrar / listar oferta del período. |
| GET / PUT | /api/v1/ofertas/{ofertaId} | Consultar / actualizar una oferta. |
| GET | /api/v1/instituciones/{institucionId}/historial | Consultar también las acciones Aula.*, Turno.* y Oferta.*. |

Registrar un aula o turno recibe { "nombre": "Aula 1" } o { "nombre": "Mañana" }. Ambas operaciones devuelven 201 e Id; Location apunta a la colección institucional donde puede consultarse el recurso.

Después de crear institución, período, aula y turno, registrar la oferta usando sus Id reales:

```json
{
  "aulaId": "00000000-0000-0000-0000-000000000001",
  "turnoId": "00000000-0000-0000-0000-000000000002",
  "capacidad": 20,
  "disponible": true,
  "referenciaCapacidad": "Acta del establecimiento",
  "origenOcupaciones": "Relevamiento manual confirmado",
  "ocupaciones": [
    { "sala": 3, "cantidad": 4 },
    { "sala": 4, "cantidad": 2 }
  ],
  "motivo": "Carga inicial de la oferta"
}
```

Los identificadores anteriores son ilustrativos: deben reemplazarse. El período se indica en la ruta. Para modificar, enviar capacidad, disponible, referenciaCapacidad, origenOcupaciones, ocupaciones, motivo y revision, sin aulaId/turnoId. La lista de ocupaciones reemplaza la anterior por completo.

Errores: 400 datos inválidos, 401 sin autenticación, 403 sin permiso, 404 recurso inexistente y 409 combinación repetida o edición desactualizada.

## Base de datos y migración

La migración AulasTurnosOferta crea configuracion.aulas, configuracion.turnos, configuracion.ofertas_aula_turno y configuracion.ocupaciones_iniciales, con relaciones, índice único y revisión de concurrencia. Oferta e historial se guardan en una misma transacción.

ConfiguracionCompleta.sql incluye ahora las tres migraciones de Configuración y admite aplicación idempotente en PostgreSQL. InicialConfiguracion.sql conserva su alcance original.

Las migraciones solo se aplicaron a PostgreSQL aislado de pruebas. No se modificó jardin_inscripciones ni otra base de la aplicación.

Desde back, con ConnectionStrings__Configuracion configurada para la base elegida, el usuario puede aplicar las migraciones:

```powershell
dotnet ef database update --project src/Modulos/Configuracion/Infrastructure --startup-project src/Modulos/Configuracion/Infrastructure --context ContextoConfiguracion
```

## Comprobaciones y estado

Implementado: operaciones anteriores, validaciones, permisos, auditoría, persistencia y concurrencia.

Probado: registro y reconstrucción; una misma aula en dos turnos; duplicados en Application y base; datos omitidos/inválidos; recursos ajenos; cambios de ocupaciones; capacidad neta inicial; historial; rechazo sin permisos; conflictos de revisión y rollback de oferta/ocupaciones/historial.

La suite completa incluye HU-01, HU-02 y HU-03. Resultado: 116 comprobaciones con SQLite y 136 con la variante PostgreSQL aislada. Compilación sin errores ni advertencias. La herramienta global dotnet-ef informa que su versión 10.0.12 es anterior al runtime 10.0.13; las migraciones se generaron y aplicaron correctamente en pruebas.

```powershell
dotnet build Jardin.sln
dotnet run --project pruebas/Jardin.Configuracion.Pruebas
```

Pendiente: asociación de versiones al período, relación completa entre límites de aula/sección/turno, tratamiento de cambios con ingresos confirmados y conciliación de ocupaciones iniciales con ingresos posteriores. Están detallados en [Cosas pendientes.md](<Cosas pendientes.md>). No declarar cerradas esas integraciones por disponer de esta primera versión.

La apertura de HU-02 conserva la omisión aprobada: todavía permite abrir sin oferta/reglas válidas. No se modificaron Git ni Trello.

