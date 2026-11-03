# HU-02 — Primera versión de períodos

Fecha: 2 de noviembre de 2026. Módulo: Configuración.

## Qué hace esta versión

Permite crear un período para un jardín y un ciclo lectivo, consultar sus fechas y estado, abrir la recepción y cerrarla. Incluye permisos institucionales, persistencia, historial y control de cambios concurrentes.

**Por decisión expresa del usuario, abrir todavía no exige reglas ni oferta válidas.** Esa comprobación y la asociación de versiones quedan en [Cosas pendientes.md](<Cosas pendientes.md>), junto con la integración pendiente de HU-01.

## Capas y archivos

| Parte | Responsabilidad |
|---|---|
| Domain/Entidades/PeriodoInscripcion.cs | Identidad, institución, ciclo, fechas y estados válidos. |
| Contracts/IModuloPeriodos.cs | Operaciones públicas de períodos del mismo módulo Configuración. No es otro módulo. |
| Contracts/...Peticion y ...Respuesta | Datos de entrada y salida con la convención acordada. |
| Application/Servicios/ServicioPeriodos.cs | Autoriza y coordina creación, consultas y cambios de recepción. |
| Application/Persistencia/IRepositorioPeriodos.cs | Acuerdo de persistencia. |
| Infrastructure/Persistencia/RepositorioPeriodos.cs | Guardado EF Core del período junto con su historial. |
| API/Controllers/PeriodosController.cs | Recibe HTTP, devuelve resultados y errores. |

ServicioPeriodos se separó de ServicioConfiguracion para no concentrar todas las funcionalidades del módulo en la clase de instituciones.

## Estados

```text
Preparado → RecepcionAbierta → RecepcionCerrada
```

Solo se abre un período preparado. Solo se cierra una recepción abierta. Intentar repetir una transición inválida devuelve 409; si la revisión enviada ya no es la actual, también devuelve 409.

RecepcionCerrada no significa que terminaron los ingresos o la distribución. No se implementó reapertura ni finalización.

## Datos y validaciones

- InstitucionId corresponde a una institución existente.
- Ciclo es un año entre 1 y 9999.
- Inicio y Fin son DateOnly: fechas sin hora.
- Ambas fechas son obligatorias y Fin no puede ser anterior a Inicio.
- Se admite un período de un solo día.
- No se impone que las fechas pertenezcan al mismo año que el ciclo.
- Revision comienza en 1 y aumenta ante apertura/cierre.
- Cada cambio de recepción exige la revisión que vio el cliente y un motivo.

La creación exige permiso de configuración; la apertura/cierre corresponde a Directivo con acceso institucional o Root. Administrativo con acceso puede consultar. Usuarios vigentes y permisos se obtienen del mismo contexto usado en HU-01.

## Endpoints

| Método y ruta | Operación |
|---|---|
| POST /api/v1/instituciones/{institucionId}/periodos | Crear un período preparado; devuelve 201, Id y Location. |
| GET /api/v1/instituciones/{institucionId}/periodos | Listar períodos del jardín. |
| GET /api/v1/periodos/{periodoId} | Consultar fechas, ciclo, estado y revisión. |
| POST /api/v1/periodos/{periodoId}/abrir | Abrir recepción manualmente. |
| POST /api/v1/periodos/{periodoId}/cerrar-recepcion | Cerrar recepción manualmente. |

Creación:

```json
{
  "ciclo": 2027,
  "inicio": "2026-11-01",
  "fin": "2026-12-15"
}
```

Apertura, luego de consultar la revisión actual:

```json
{
  "revision": 1,
  "motivo": "Comenzar la recepción del prototipo"
}
```

Una apertura correcta devuelve estado RecepcionAbierta y revisión 2. Para cerrar se debe enviar esa nueva revisión.

## Historial y persistencia

La tabla configuracion.periodos tiene una relación con la institución y una revisión de concurrencia. El período y su registro de historial se guardan juntos.

Se reutilizó el historial institucional para no crear un segundo mecanismo. Las acciones son Periodo.Creacion, Periodo.Apertura y Periodo.CierreRecepcion; las instantáneas JSON incluyen el Id del período. Se consultan en el endpoint de historial del jardín.

La migración PeriodosInscripcion se agrega después de InicialConfiguracion. El script ConfiguracionCompleta.sql incluye ambas. El archivo InicialConfiguracion.sql conserva solo la primera migración.

Desde back, configurar ConnectionStrings__Configuracion y aplicar las migraciones con la fábrica existente:

```powershell
dotnet ef database update --project src/Modulos/Configuracion/Infrastructure --startup-project src/Modulos/Configuracion/Infrastructure --context ContextoConfiguracion
```

No se ejecutó esta orden contra la base de la aplicación; se probaron las migraciones únicamente en PostgreSQL aislado.

## Comprobaciones

```powershell
dotnet build Jardin.sln
dotnet run --project pruebas/Jardin.Configuracion.Pruebas
```

La suite incluye HU-01 y HU-02: creación/consulta, fechas y ciclo inválidos, institución inexistente, estados, permisos, historial y conflictos de revisión. Una prueba comprueba expresamente que el prototipo puede abrir sin oferta/reglas, para que la omisión no quede escondida.

Resultado: 74 comprobaciones con SQLite y 89 con la variante PostgreSQL aislada. La solución compila con cero errores y cero advertencias. El modelo coincide con las migraciones.

No se modificó Trello ni se ejecutaron comandos de Git. Para el alcance pendiente de cada historia, consultar Cosas pendientes.md.
