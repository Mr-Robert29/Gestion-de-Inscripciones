# Migraciones EF Core — guía de trabajo

Los cinco módulos usan Entity Framework Core para registrar y aplicar sus cambios de estructura. Cada uno tiene su contexto, migraciones y snapshot dentro de Infrastructure/Persistencia/Migraciones, y su historial en PostgreSQL.

| Módulo | Contexto | Historial |
| --- | --- | --- |
| Usuarios | ContextoUsuarios | usuarios.__EFMigrationsHistory |
| Configuración | ContextoConfiguracion | configuracion.__EFMigrationsHistory |
| Inscripciones | ContextoInscripciones | inscripciones.__EFMigrationsHistory |
| Vacantes | ContextoVacantes | vacantes.__EFMigrationsHistory |
| Secciones | ContextoSecciones | secciones.__EFMigrationsHistory |

## Qué ocurre en tu base actual

Configuración e Inscripciones ya tenían sus historiales. Para los otros tres módulos se crearon migraciones iniciales completas y se registraron como aplicadas solo después de comparar la estructura existente con una base de referencia. No se ejecutaron sus CreateTable sobre las tablas ya existentes ni se borraron datos.

La adopción agregó tres tablas de historial. El entorno local tiene ahora 54 tablas en total, de las cuales cinco son historiales de EF. Las migraciones anteriores de Configuración e Inscripciones se conservan sin reemplazarlas ni borrar su historial.

## Si clonás el repositorio en otra computadora

1. Instalar el SDK de .NET 10 y PostgreSQL; crear jardin_inscripciones vacía.
2. Configurar secretos de usuario en Jardin.Api: conexión, JWT y clave inicial de Root si corresponde.
3. Desde la raíz del repositorio, con la API detenida:

```powershell
dotnet tool restore
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --actualizar-base
```

Después iniciar normalmente el proyecto. El frontend requiere Python 3. --actualizar-base no crea instituciones ni solicitudes y tampoco crea Root: la API inicializa Root al arrancar. La herramienta usa DefaultConnection de los secretos de Jardin.Api y exige una única conexión para los módulos.

## Qué hago después de bajar cambios

También podés aplicar las pendientes con doble clic en `actualizar-base.bat`, ubicado en la raíz del repositorio. Usa rutas relativas, lee los secretos de Jardin.Api y mantiene la ventana abierta para mostrar el resultado. Detener el backend antes de ejecutarlo; PostgreSQL debe estar encendido y la base debe existir. No reemplaza una copia de seguridad antes de cambios importantes. `actualizar-base.bat --comprobar` verifica rutas y SDK sin ejecutar migraciones.

Para consultar el estado:

```powershell
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --estado-migraciones
```

Si hay nuevas migraciones:

```powershell
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --actualizar-base
```

Cada migración se aplica una sola vez por base. Repetir la actualización no repite cambios ya aplicados. Guardar una copia antes de actualizar una base importante y revisar el contenido de las nuevas migraciones: algunas futuras pueden incluir cambios destructivos que requieren una decisión específica.

No borrar la base, los archivos de migraciones aplicadas ni las filas del historial para actualizar. El arranque normal y el .bat no aplican migraciones automáticamente.

## Cómo crear una migración nueva

Ejemplo: después de modificar una entidad de Vacantes, desde la raíz:

```powershell
dotnet ef migrations add NombreClaroDelCambio --project back/src/Modulos/Vacantes/Infrastructure --context ContextoVacantes --output-dir Persistencia/Migraciones
```

Revisar el archivo generado y probarlo antes de aplicarlo. Cambiar módulo y contexto para los demás. La herramienta dotnet-ef queda fijada a la versión 10.0.13 mediante dotnet-tools.json.

Las fábricas de diseño permiten generar migraciones sin iniciar la API, cargar cuentas ni conectarse a la base del usuario. Usan ConnectionStrings__NombreModulo, luego ConnectionStrings__DefaultConnection o una conexión de diseño sin contraseña. No leen automáticamente los secretos de Jardin.Api. Para actualizar con esos secretos, usar el comando conjunto --actualizar-base indicado arriba.

Los snapshots son generados por EF: representan el modelo que conoce la última migración. No hay que escribirlos ni editarlos manualmente para evitar un cambio de tablas.

## Bases antiguas sin historial en Usuarios/Vacantes/Secciones

Únicamente para bases creadas con la instalación anterior:

```powershell
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --adoptar-base-existente
```

Primero guardar una copia. Este comando necesita permiso para crear y eliminar su propia base temporal de referencia. Compara la estructura y rechaza diferencias; no intenta corregirlas ni marcar migraciones desconocidas. Si la comprobación falla, conservar la base y preparar una actualización específica. No asumir que tener tablas con el mismo nombre garantiza que el modelo coincida.

## Comprobar el trabajo

```powershell
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --pruebas-migraciones
```

La primera suite utiliza SQLite aislado. La segunda usa las credenciales configuradas solo para crear bases PostgreSQL temporales propias: comprueba instalación nueva, reintento, adopción de tablas con registros y rechazo de estructura incompatible sin historiales parciales. No inserta datos ficticios en jardin_inscripciones.

Referencia del procedimiento de historial y migraciones: [Microsoft — Managing Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing).
