# Continuidad — 2 de noviembre de 2026

Leer Reglas.md antes de editar código. El usuario pidió completar HU-01 y confirmó como obligatorios nombre, calle, número, localidad, provincia y teléfono. Código, correo y coordenadas siguen siendo opcionales.

## Avance realizado

Se implementaron registro, listado, consulta, actualización e historial de instituciones en Configuración, persistencia EF Core/Npgsql y endpoints HTTP autenticados.

Se agregaron permisos por institución en Application y un adaptador del host que obtiene al usuario de su sesión vigente. Root mantiene acceso completo; los usuarios no pueden elegir el autor del historial desde la petición.

Se incorporó RevisionDatos como control de edición concurrente. RevisionUbicacion conserva su responsabilidad de identificar cambios de domicilio/coordenadas.

Institución e historial se guardan juntos. El historial conserva actor, fecha, motivo e instantáneas anteriores/nuevas. Los cambios se consultan mediante una operación de solo lectura.

Se generó y comprobó la migración InicialConfiguracion y su script SQL. No se aplicaron a las bases de la aplicación.

Se hicieron dos ajustes necesarios en Usuarios: consultar roles globales actuales de Identity al obtener la sesión, y mapear AccesoInstitucion.Id como generado por el dominio para insertar correctamente accesos nuevos.

## Límite para cerrar HU-01

No se declara toda la historia terminada. Inscripciones, Vacantes y Secciones todavía no tienen los procesos reales de mediciones y propuestas.

Configuración tiene un índice UsoUbicacion con referencias y revisiones, y los contratos RegistrarUsoUbicacionAsync, ConsultarImpactoUbicacionAsync y ComprobarVigenciaUsoAsync. Permiten detectar referencias que usaron otra ubicación.

Se comprobó con referencias ficticias persistidas. Falta que los productores reales registren sus usos, distingan resultados vigentes de históricos y bloqueen sus confirmaciones de forma consistente. La consulta de vigencia aislada no garantiza una confirmación atómica si la ubicación cambia entre ambas operaciones.

No implementar de golpe las demás historias ni inventar sus reglas pendientes para declarar cerrada HU-01. El estado completo y los comandos están en HU-01-Implementacion.md.

## Verificaciones finales

- Jardin.sln: 22 proyectos compilados con 0 errores y 0 advertencias.
- Suite incorporada en pruebas/Jardin.Configuracion.Pruebas.
- 46 comprobaciones con HTTP, Identity/JWT real y SQLite aislado.
- 58 comprobaciones con persistencia de Configuración en PostgreSQL temporal y Usuarios en SQLite aislado.
- Migraciones: no hay cambios del modelo pendientes respecto del snapshot.
- Se probó rollback de datos cuando falla el historial y rechazo de ediciones concurrentes.
- Se probaron permisos, revocación de acceso/rol y cuenta desactivada.

El servidor PostgreSQL temporal se detuvo al terminar. No se cambió el servicio PostgreSQL habitual ni las bases de la aplicación. El rol temporal, la clave ficticia y los registros de prueba pertenecían exclusivamente a esa instancia aislada.

La herramienta dotnet-ef instalada es 10.0.12 y avisa que el runtime es 10.0.13; pudo generar y validar la migración correctamente. No se actualizó la herramienta global.

## Próximo paso

Revisar con el usuario el alcance implementado y leer HU-01-Implementacion.md. Para probar manualmente contra su base local, explicar primero cómo configurar ConnectionStrings__Configuracion y aplicar la migración de Configuración. No hacerlo sobre una base existente sin revisar la conexión y su esquema.

El usuario maneja Git y GitHub. No se ejecutaron comandos Git, no se hizo commit y no se modificó Trello en esta intervención. La tarjeta de HU-01 no fue marcada como terminada ni sus casillas actualizadas.

