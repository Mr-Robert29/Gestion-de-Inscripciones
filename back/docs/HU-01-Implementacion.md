# HU-01 — Estado de implementación y uso

Fecha: 2 de noviembre de 2026. Reemplaza como referencia actual al primer recorrido documentado el 27 de octubre.

## Resultado y límite de alcance

Configuración permite registrar, listar, consultar y actualizar instituciones por HTTP, aplicar permisos institucionales y guardar historial en PostgreSQL. Se generó una migración propia del esquema configuracion.

Hay un índice persistente de referencias a ubicaciones y una consulta que identifica las afectadas por cambio de revisión. Los contratos permiten registrar usos y comprobar su vigencia.

**La historia no debe marcarse completamente terminada todavía:** los procesos reales que crean mediciones y confirman propuestas aún no existen en Inscripciones, Vacantes y Secciones. El índice no puede descubrir registros de esos módulos sin que se integren. Se comprobó el rechazo de una referencia obsoleta; no una confirmación real de ingreso/distribución, porque esa operación todavía no está implementada.

## Criterios de aceptación

| Criterio | Implementación y evidencia |
|---|---|
| Registrar y consultar | POST, GET individual y GET de lista; guardado y reconstrucción comprobados con SQLite y PostgreSQL aislados. |
| Registrar sin coordenadas | Coordenadas null se conservan como pendientes; no se inventa distancia cero. |
| Actualizar | PUT conserva el Id. RevisionDatos evita sobrescribir una edición concurrente; devuelve 409 ante conflicto. |
| Bloquear sin permiso | JWT, cuenta activa, roles actuales y accesos institucionales. Se comprueba también en Application. |
| Conservar historial | Alta y edición guardan actor, fecha, motivo e instantáneas JSON. Un único SaveChanges conserva datos e historial en una transacción. |
| Revisar consecuencias | Índice y consultas de vigencia implementados y probados con referencias ficticias. Falta conectar productores y confirmaciones reales de otros módulos. |

## Campos confirmados

El usuario confirmó nombre, calle, número, localidad, provincia y teléfono obligatorios. No se aceptan null, texto vacío ni solo espacios en esos datos.

Código, correo y coordenadas siguen siendo opcionales. No se inventaron formatos de teléfono, unicidad de código ni un formato institucional específico. Las coordenadas tienen validación de rangos numéricos.

El primer jardín debe registrarse con Root o una cuenta con permiso de configuración. Root consulta y modifica todas las instituciones. Administrativo puede consultar las instituciones asignadas; Directivo puede modificarlas cuando tiene acceso. Los roles globales vigentes de Identity se consultan desde Usuarios; no se confía únicamente en roles viejos del JWT.

## Recorrido por capas

1. **API:** InstitucionesController recibe HTTP y llama a IModuloConfiguracion.
2. **Contracts:** Peticion indica qué entra; Respuesta indica qué sale; Datos agrupa información.
3. **Application:** ServicioConfiguracion obtiene la identidad del contexto autenticado, autoriza, transforma DTO y ejecuta el caso de uso.
4. **Domain:** Institucion y sus objetos de valor validan datos y mantienen identidad, estado y revisiones.
5. **Infrastructure:** RepositorioInstituciones y ContextoConfiguracion guardan institución e historial con EF Core/Npgsql.

La identidad proviene de la sesión del servidor. El cliente no puede elegir el UsuarioId autor del historial.

## Revisiones: dos propósitos distintos

- **RevisionUbicacion:** cambia cuando cambian domicilio o coordenadas. Permite identificar resultados calculados con otra ubicación.
- **RevisionDatos:** cambia ante modificaciones. El cliente devuelve su revisión en el PUT; si es anterior a la actual, se rechaza la edición.

Si dos usuarios leen la misma revisión y editan simultáneamente, EF comprueba también la revisión al escribir. La edición que pierde el conflicto no guarda ni datos ni historial.

ActualizarInformacion recibe el estado completo. Si cambia el domicilio, el cliente debe enviar coordenadas correctas para esa dirección o null para dejarlas pendientes. Configuración no consulta Geoapify en HU-01: la localización automática corresponde al trabajo posterior de Inscripciones.

## Endpoints

Todos requieren autenticación. La ruta base es /api/v1/instituciones.

| Método y ruta | Resultado |
|---|---|
| POST /api/v1/instituciones | 201 y RegistrarInstitucionRespuesta con Id y cabecera Location. |
| GET /api/v1/instituciones | Lista filtrada por accesos; Root ve todas. |
| GET /api/v1/instituciones/{id} | Consulta de datos y revisiones; 404 si no existe y el usuario tiene alcance para consultarla. |
| PUT /api/v1/instituciones/{id} | ActualizarInstitucionRespuesta con Id y revisiones guardadas. |
| GET /api/v1/instituciones/{id}/historial | Cambios ordenados por fecha, con actor e instantáneas. |
| GET /api/v1/instituciones/{id}/impacto-ubicacion | Ubicación pendiente y referencias que utilizaron una revisión distinta. |

Errores previstos: 400 por entrada inválida, 401 sin autenticación válida, 403 por falta de permiso/cuenta desactivada, 404 por institución inexistente y 409 por conflicto de edición. Una operación cancelada no se convierte en un error de validación.

El motivo de actualización es obligatorio para reconstruir por qué se modificó la institución.

### Ejemplo de alta

```json
{
  "nombre": "Jardin de ejemplo",
  "domicilio": {
    "calle": "San Martin",
    "numero": "123",
    "localidad": "La Plata",
    "provincia": "Buenos Aires"
  },
  "telefonoContacto": "12345",
  "codigo": null,
  "correoElectronicoContacto": null,
  "coordenadas": null
}
```

Para modificar, consultar primero el registro y enviar sus datos completos junto con revisionDatos y motivo. Las coordenadas null en el PUT significan que quedan pendientes; no es una actualización parcial que conserve un valor omitido.

## Preparar la base local

Configurar ConnectionStrings__Configuracion fuera del código. Si no está definida, se utiliza DefaultConnection de la API. La fábrica de migraciones utiliza ConnectionStrings__Configuracion y no carga automáticamente .env.

Desde back, después de configurar esa variable y revisar la conexión:

```powershell
dotnet ef database update --project src/Modulos/Configuracion/Infrastructure --startup-project src/Modulos/Configuracion/Infrastructure --context ContextoConfiguracion
```

Esta orden aplica solamente la migración de Configuración en la base elegida. No crea las tablas de Usuarios ni convierte datos de una estructura anterior. La API no aplica migraciones de Configuración automáticamente al arrancar.

La migración y el script SQL están en src/Modulos/Configuracion/Infrastructure/Persistencia/Migraciones. Las credenciales reales no deben agregarse al repositorio.

Durante esta implementación se aplicó la migración únicamente en una instancia temporal y aislada de PostgreSQL, no en la base local de la aplicación.

## Integración pendiente de mediciones y propuestas

El índice UsoUbicacion conserva InstitucionId, ReferenciaId, Tipo y RevisionUbicacion. No almacena la distancia ni las decisiones de una propuesta y no sustituye las tablas del módulo de origen.

Tipos admitidos: MedicionDistancia, PropuestaIngreso y PropuestaDistribucion.

Cuando existan los procesos de origen:

1. Inscripciones guardará las coordenadas y revisión usadas en cada medición, sin sobrescribir una medición histórica.
2. El productor deberá registrar su referencia mediante RegistrarUsoUbicacionAsync.
3. Las propuestas deben conservar las revisiones de entrada utilizadas.
4. Antes de confirmar, el caso de uso debe consultar ComprobarVigenciaUsoAsync y rechazar una referencia inexistente, pendiente u obsoleta.
5. La comprobación de vigencia y la confirmación deben coordinarse para evitar que cambie la ubicación entre ambas operaciones. El contrato actual no proporciona por sí solo una transacción compartida entre módulos.
6. El productor deberá distinguir referencias históricas de resultados vigentes: el índice actual conserva las referencias registradas y la consulta informa las afectadas. No conoce por sí solo el estado de negocio de una propuesta.

No hay un endpoint HTTP para registrar estas referencias: son operaciones internas entre módulos. La consulta de impacto no recalcula ni confirma nada.

## Cambios necesarios en Usuarios y el host

- Se agregó ObtenerRolesAsync en IServicioIdentidad y su implementación. ObtenerSesionActualAsync ahora incluye los roles globales actuales, indispensable para reconocer a Root sin accesos institucionales y rechazar un rol revocado.
- Se indicó ValueGeneratedNever para AccesoInstitucion.Id: el dominio ya genera esos identificadores. La prueba de integración detectó que EF intentaba actualizar un acceso nuevo en lugar de insertarlo.
- Program registra Configuración y su contexto de usuario. En entorno Pruebas se omiten el archivo .env local y la inicialización automática de cuentas.
- No se reescribieron el resto de Usuarios ni sus operaciones.

## Verificaciones reproducibles

Desde back:

```powershell
dotnet build Jardin.sln
dotnet run --project pruebas/Jardin.Configuracion.Pruebas
```

Sin configuración adicional, la suite usa SQLite aislado en memoria para ambos contextos y prueba los endpoints con Identity/JWT reales.

Para comprobar además PostgreSQL, definir HU01_POSTGRES_PRUEBAS con una base exclusiva cuyo nombre comience con hu01_pruebas_. La suite aplica allí la migración de Configuración; nunca utilizar la base de la aplicación para esta variable. Usuarios conserva SQLite aislado en estas pruebas.

Se verificaron alta, consulta, validaciones, edición, historial, permisos, revocación de acceso y rol, conflictos de edición, rollback ante fallo de historial y referencias de ubicación obsoletas. La suite está incorporada a la solución como un ejecutable de comprobaciones; se ejecuta con dotnet run, no con dotnet test.

Resultado final de esta intervención: 46 comprobaciones con SQLite y 57 con la variante PostgreSQL. La solución compiló con cero errores y cero advertencias. Se verificó que el modelo coincide con la migración generada. La instancia temporal PostgreSQL se detuvo al terminar, sin cambiar las bases de la aplicación.

Las referencias ficticias demuestran los contratos de impacto; no demuestran la integración con historias aún no implementadas.

## Estado de aprendizaje y continuidad

**Confirmado:** campos obligatorios y opcionales indicados, nomenclatura de DTO, formato y comentarios de interfaces.

**Implementado:** funcionalidades de Configuración descritas arriba y contratos de integración.

**Comprobado:** persistencia y HTTP con bases aisladas; consultar la salida de la suite para el número de comprobaciones.

**Pendiente para cerrar toda HU-01:** integrar productores de mediciones y propuestas, sus estados vigentes y la confirmación consistente. No marcar la tarjeta completa ni todas sus casillas solo por compilar.

No se ejecutaron comandos de Git ni se modificó Trello. El usuario conserva el control del commit y la PR.

### Referencias técnicas utilizadas

- [Tipos dependientes y objetos de valor en EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities).
- [Conflictos de concurrencia de EF Core](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).
- [Pruebas de integración de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0).

