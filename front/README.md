# Frontend de Jardín

Primera versión de la interfaz del sistema de inscripciones. Proyecto separado del backend, construido con **HTML, CSS con BEM y JavaScript sin frameworks**. No requiere instalar dependencias para ejecutarse.

## Abrir la aplicación

Desde una terminal ubicada en `front`, ejecutá:

```powershell
npm start
```

Ese comando usa Python para servir los archivos. También podés ejecutarlo directamente, sin npm:

```powershell
python -m http.server 4173 --bind 127.0.0.1
```

Abrí <http://127.0.0.1:4173>. No abras `index.html` con doble clic: JavaScript utiliza módulos que necesitan un servidor HTTP.

**Para ver el diseño sin backend**, elegí “Explorar la vista previa”. Los datos son ficticios, no se consulta la API y el guardado está deshabilitado.

## Iniciar sesión con el backend

1. Iniciá la API siguiendo las instrucciones del backend y verificá su conexión a PostgreSQL y sus migraciones.
2. En el frontend, desplegá “Configurar conexión al servidor”. La dirección predeterminada es `http://localhost:5167`, sin agregar `/api/v1`.
3. Ingresá con una cuenta existente y habilitada del backend. Este frontend no crea cuentas ni trae contraseñas predeterminadas.
4. Seleccioná institución y período en el encabezado.

El frontend utiliza `fetch` para enviar peticiones HTTP y JWT para autenticar las operaciones. El token permanece en memoria: **recargar la página cierra la sesión**. No se guarda en localStorage ni sessionStorage. Los permisos definitivos los verifica el backend.

Si la API utiliza otra dirección, también podés cambiar el valor inicial en `js/configuracion.js`. Una dirección HTTP es para desarrollo local; la publicación requiere HTTPS y revisar CORS del backend.

## Qué incluye

| Pantalla | Operaciones preparadas para la API |
| --- | --- |
| Acceso | Iniciar sesión, consultar acceso y cerrar sesión. |
| Inicio | Resumen de la consulta del período y accesos rápidos. Los contadores no representan totales globales. |
| Mi institución | Registrar, consultar y modificar datos, domicilio y coordenadas opcionales. |
| Períodos | Crear, consultar, abrir/cerrar recepción y publicar/asignar versiones de reglas y oferta. |
| Oferta | Registrar aulas, turnos y oferta con capacidad y ocupaciones iniciales; consultar la oferta. |
| Solicitudes | Buscar por nombre, apellido o DNI, paginar, registrar aspirante y solicitud, o reutilizar un aspirante por identificador. |
| Ficha | Consultar datos, corregir domicilio y preferencia principal, y agregar/vincular responsables. |
| Documentación | Registrar y revisar documentación y condiciones declaradas. |
| Domicilio y distancia | Registrar revisión manual, cargar metros manualmente y consultar historial. |
| Duplicados | Detectar alertas y registrar decisiones con motivo, según permisos. |
| Vacantes y Secciones | Presentación visual; las operaciones todavía no están integradas. |

Los turnos al crear una solicitud se obtienen de la versión de oferta publicada y asignada al período, no de cambios sin publicar. La carga manual de distancia conserva su literal decimal al enviarlo; no implementa Geoapify ni Haversine.

## Estructura

```text
front/
  index.html
  package.json
  css/
    variables.css          Colores, tamaños y valores compartidos
    base.css               Estilos generales y accesibilidad
    estructura.css         Menú, encabezado y distribución adaptable
    componentes.css        Botones, formularios, tablas y ventanas
    paginas.css            Acceso, inicio y pantallas particulares
  js/
    configuracion.js       Dirección inicial del backend
    api.js                 HTTP, JWT y errores
    aplicacion.js          Sesión, navegación y contexto
    interfaz.js            Elementos visuales y escape de texto
    compartidos.js         Listados y componentes compartidos
    pagina-inicio.js
    pagina-configuracion.js
    pagina-solicitudes.js
    pagina-ficha.js
    paginas.js             Selección de pantalla y páginas restantes
    acciones.js            Formularios y operaciones de escritura
    datos-previa.js        Datos ficticios del recorrido visual
  recursos/
    marca.svg
  pruebas/
    api.test.js
  docs/
    README.md              Decisiones, comprobaciones y pendientes
```

## Comprobar

Con Node.js disponible, desde `front`:

```powershell
npm test
```

Las cinco pruebas comprueban precisión del envío decimal, rechazo de literales inválidos, escape de texto, autenticación HTTP y descarte de respuestas de sesiones anteriores. No requieren paquetes adicionales.

Para el estado de la integración y sus límites, consultá [la documentación del frontend](docs/README.md).
