# Instalación y migraciones de los cinco módulos

Usuarios, Configuración, Inscripciones, Vacantes y Secciones tienen migraciones EF Core y su propio __EFMigrationsHistory en el esquema correspondiente. Los archivos fuente están en Infrastructure/Persistencia/Migraciones de cada módulo. Los SQL de esta carpeta se generan desde esas migraciones; no son otro modelo independiente.

## Base nueva

1. Crear la base vacía jardin_inscripciones y configurar los secretos de Jardin.Api.
2. Desde la raíz ejecutar:

```powershell
dotnet tool restore
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --actualizar-base
```

El comando aplica solo las migraciones pendientes de los cinco módulos. La base debe existir y las credenciales deben tener permisos para crear sus esquemas y tablas. No se aplican automáticamente al arrancar la API o el .bat.

Alternativa para la instalación inicial en una única transacción: --preparar-base-vacia. Rechaza cualquier base que ya tenga tablas de los módulos. --generar-instalacion actualiza los cinco SQL idempotentes de esta carpeta sin ejecutarlos.

## Base existente creada con los scripts anteriores

Si Usuarios, Vacantes y Secciones tienen tablas pero no historial EF, no ejecutar sus migraciones iniciales directamente.

1. Guardar una copia de seguridad.
2. Ejecutar --adoptar-base-existente.
3. Ejecutar --estado-migraciones y después --actualizar-base.

La adopción compara la estructura con una base temporal creada a partir de las migraciones iniciales. Comprueba tablas, columnas, tipos, nulabilidad, valores por defecto, restricciones, índices y triggers. Si hay diferencias, rechaza todo. Si coincide, solo crea los tres historiales y registra sus migraciones iniciales, verificando huellas de los datos antes y después en la misma transacción. No recrea tablas de negocio ni borra registros. Repetirla tras una adopción completa no hace cambios.

La adopción y las pruebas PostgreSQL necesitan permiso de crear una base temporal en el servidor. La temporal usa un nombre generado jardin_migraciones_prueba_* y se elimina al finalizar; no se elimina la base del usuario. Si ese permiso no está disponible, debe coordinarse la comprobación con quien administra PostgreSQL.

## Históricos

Los SQL incrementales antiguos conservados dentro de Infrastructure son antecedentes. No ejecutarlos después de esta instalación ni de la adopción: sus cambios ya forman parte de las migraciones iniciales. Para cambios futuros, crear una nueva migración del módulo y aplicar --actualizar-base.

Guía y ejemplos: ../Migraciones.md.
