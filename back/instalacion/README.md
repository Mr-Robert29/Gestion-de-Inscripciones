# Instalación inicial de una base vacía

Scripts generados el 5 de noviembre de 2026 para jardin_inscripciones. No contienen datos de conexión ni contraseñas.

- 01_usuarios.sql: modelo actual de Identity y personal.
- 02_configuracion.sql: migraciones EF de Configuración, con su historial.
- 03_inscripciones.sql: migraciones EF de Inscripciones, con su historial.
- 04_vacantes.sql: modelo actual de Vacantes, incluido HU-21.
- 05_secciones.sql: modelo actual de Secciones, incluido HU-26.

Estos scripts constituyen una instalación inicial completa. No volver a aplicar después los scripts incrementales anteriores de Vacantes/Secciones, porque sus columnas y tablas ya están incluidas. Los scripts de esos módulos todavía no equivalen a una cadena de migraciones EF; los cambios futuros deben prepararse y comprobarse por separado.

Desde la raíz del repositorio:

```
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --generar-instalacion
dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --preparar-base-vacia
```

El segundo comando lee la conexión exclusivamente de los secretos vinculados a Jardin.Api. Comprueba el nombre de la base, toma protección de instalación y rechaza si ya hay tablas en los esquemas del proyecto. Ejecuta todo en una transacción; no borra ni reinicializa una base existente. No es una operación automática al iniciar la API.

Instalación aplicada al entorno local: 51 tablas en cinco esquemas. La conexión se comprobó con una consulta de solo lectura antes de instalar. La creación de la cuenta Root se realiza después al iniciar la API, con la contraseña configurada por el usuario, y está sujeta a las reglas de Identity.
