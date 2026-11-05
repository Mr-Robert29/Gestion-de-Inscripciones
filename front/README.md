# Frontend de Jardín

Primera versión funcional en HTML, CSS con BEM y módulos JavaScript. El frontend se comunica por HTTP con la API; nunca se conecta directamente a PostgreSQL.

## Iniciar

1. Configurar el backend siguiendo `../back/README.md`. Copiar `../back/.env.example` como `../back/.env` y completar sus valores localmente. No compartir ni subir claves o contraseñas.
2. Preparar los esquemas y migraciones del backend en la base elegida. No confundir el servidor de pruebas con la base real.
3. Desde la raíz del repositorio, iniciar la API con `dotnet run --project back/src/Jardin.Api --launch-profile http`.
4. En otra terminal, desde `front`, ejecutar `npm start` o `python -m http.server 4173 --bind 127.0.0.1`.
5. Abrir http://127.0.0.1:4173. La API predeterminada es http://localhost:5167; se puede cambiar desde «Configurar conexión al servidor» en el acceso.

No abrir index.html con doble clic: los módulos necesitan HTTP.

También podés hacer doble clic en `iniciar-proyecto.bat`, ubicado junto a `back` y `front`. Inicia ambos servidores, espera que respondan y abre el navegador. Usa rutas relativas a su propia ubicación, incluso si lo ejecutás desde otra carpeta. Requiere .NET 10, Python 3, PostgreSQL encendido y secretos de usuario configurados. Evita iniciar otra instancia si los puertos están ocupados. Para detener los servidores, presioná Ctrl+C en cada ventana. La opción `--comprobar` solo valida carpetas y herramientas, sin iniciar procesos.

## Pantallas y navegación

Hay **16 pantallas principales, incluido el acceso**. Varias tienen vistas secundarias para revisar cada etapa; no son proyectos independientes.

| Pantalla | Acceso / ícono | Funciones |
| --- | --- | --- |
| Acceso | Inicio de sesión | Login real, dirección de API y vista previa sin escrituras. |
| Inicio | Casa | Resumen de solicitudes de la consulta actual y accesos rápidos. |
| Solicitudes | Documento | Búsqueda y paginación; acceso a fichas. |
| Nueva solicitud | Botón «Nueva solicitud» | Registrar o reutilizar aspirante, domicilio y preferencias. |
| Ficha de solicitud | «Ver ficha» | Consultar solicitud, corregir datos y vincular responsables. |
| Domicilios y distancias | Ubicación | Revisar domicilio y registrar metros manualmente con historial. |
| Documentación | Lista | Registrar y revisar documentos y condiciones declaradas. |
| Posibles duplicados | Personas | Detectar y resolver alertas con motivo. |
| Mi institución | Jardín | Registrar, consultar y actualizar la institución. |
| Períodos | Calendario | Crear períodos, publicar/asignar versiones y abrir/cerrar recepción. |
| Aulas, turnos y oferta | Aulas | Registrar recursos y oferta, capacidades y ocupaciones iniciales. |
| Vacantes y admisión | Personas | Prioridades, sorteo, casos manuales, propuestas, espera, ingresos y ofrecimientos. |
| Secciones | Aulas | Definir secciones, cambiar pura a multiedad, consultar historial y gestionar distribuciones. |
| Resultados e historial | Lista | Consultar resultados, ingresos, bajas, cambios y distribuciones. Exportar reporte y nómina CSV. |
| Usuarios y permisos | Personas; solo Root | Crear cuentas, habilitar/deshabilitar, asignar y revocar accesos institucionales. |
| Ayuda y estado | Ayuda | Explicación del uso y límites de esta versión. |

Las pestañas de **Vacantes** separan Propuestas, Prioridades y sorteo, Lista de espera, Ofrecimientos e Ingresos y bajas. Una propuesta tiene su propia vista de revisión. Los candidatos se consultan por aula/turno y se ofrece a una persona con motivo.

Las pestañas de **Secciones** separan la composición de los grupos y las propuestas de distribución. Cada distribución tiene su propia vista de revisión y confirmación. Los cambios de composición conservan historial y no confirman ingresos automáticamente.

## Recorrido de una primera inscripción

1. Registrar institución, aula y turno.
2. Crear período y preparar la oferta con capacidades y ocupaciones iniciales.
3. Publicar reglas y oferta y asignarlas al período. Abrir recepción.
4. Registrar aspirantes, solicitudes y responsables; revisar documentos, condiciones, duplicados y distancias manuales.
5. Cerrar recepción. Revisar prioridades y registrar el sorteo cuando corresponda. Resolver los casos manuales identificados; no asumir desempates automáticos.
6. Generar propuesta, revisar resultados y registrar la aceptación familiar de turnos alternativos. Confirmar ingresos explícitamente.
7. Definir secciones, generar distribución, revisar asignaciones y confirmar.
8. Ante una baja, registrarla desde Ingresos y bajas. Si queda pendiente de coordinación, la plaza sigue ocupada hasta completar el reintento.
9. Consultar candidatos compatibles, ofrecer una plaza, registrar aceptación/rechazo/cancelación y confirmar el ingreso aceptado. Rechazar un turno alternativo conserva la espera.
10. Si corresponde, ampliar una sección pura a multiedad antes del ofrecimiento. Generar después una distribución para los ingresos que todavía no tienen sección; se conservan los ya asignados.
11. Consultar Resultados e historial y descargar los CSV autorizados.

El servidor vuelve a validar permisos, disponibilidad, revisiones y compatibilidad al guardar. Un conflicto no autoriza a forzar la operación: cerrar el formulario, consultar de nuevo y revisar los cambios.

## Sesión y permisos

JWT solo en memoria: al recargar se cierra la sesión. No se almacena en localStorage ni sessionStorage. Root accede a todo; los demás accesos se comprueban por institución. Los botones de decisiones definitivas se limitan a Directivo/Root; el backend sigue siendo la autoridad sobre cada operación.

Las exportaciones se descargan mediante peticiones autenticadas; la URL no contiene el token. Las ofertas desde espera conservan un identificador de operación para reintentar sin duplicar reservas.

## Organización del código

- `api.js`: HTTP, JWT, errores y descarga CSV.
- `aplicacion.js`: sesión, menú, institución/período y navegación.
- `pagina-*.js`: presentación de cada grupo de pantallas.
- `acciones.js`: formularios de Configuración e Inscripciones.
- `acciones-organizacion.js`: distribución de operaciones a `acciones-admision.js`, `acciones-espera.js`, `acciones-secciones.js` y `acciones-administracion.js`.
- `operaciones-formularios.js`: formularios, protección contra doble envío y contexto cambiado.
- `organizacion-comun.js`: tablas y nombres de solicitudes, aulas y turnos.
- `interfaz.js`, `compartidos.js`: componentes y escape de texto.
- `css/`: variables, estilos base, estructura, componentes y pantallas.

## Verificaciones

Desde front: `npm test`. No requiere paquetes instalados de ejecución.

Para comprobar con la API real y SQLite descartable:

1. Desde la raíz: `dotnet run --project back/pruebas/Jardin.Configuracion.Pruebas -- --servidor-integracion`.
2. En otra terminal: `node front/pruebas/integracion-real.mjs`.
3. Finalizar el servidor de pruebas con Ctrl+C. Los datos se descartan; no usa la base del usuario.

Ese recorrido utiliza las funciones del frontend, Identity/JWT y los servicios/persistencia reales del backend. Comprueba propuestas, distribuciones, bajas, multiedad, ofrecimientos, aceptación, confirmación, reportes, CSV y cuentas. El puerto aislado es 5189; no cambiar la dirección predeterminada de la aplicación por esa dirección de pruebas.

También se verificaron en navegador login, menú móvil, consulta de sección multiedad, historial, listado de distribuciones y guardado de un formulario. En 389 px no hubo desbordamiento horizontal de la página ni errores de consola en ese recorrido.

## Límites de la primera versión

- Geoapify permanece pendiente: domicilio y distancia se revisan/cargan manualmente.
- La prueba contra el PostgreSQL del usuario requiere completar la configuración local y verificar/aplicar los esquemas necesarios. SQLite no valida los scripts de PostgreSQL ni los bloqueos entre instancias.
- Recuperación/cambio de contraseña, edición de la persona compartida, edición de oferta existente y algunos ajustes de configuración no tienen formulario en esta entrega. No se inventan operaciones sin contrato.
- Fuera de término, retiro voluntario de espera y partición de capacidad de varias secciones por aula/turno mantienen los pendientes documentados del backend.
- La vista previa sigue sin guardar ni consultar la API. No confundir sus datos con resultados reales.
- Algunas consultas de organización cargan el período completo; la paginación adicional sigue pendiente para volúmenes grandes.
