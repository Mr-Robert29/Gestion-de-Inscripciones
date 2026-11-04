# Estado del frontend

## Decisiones aplicadas

- HTML semántico, CSS con BEM y módulos de JavaScript. Sin dependencias de ejecución.
- Interfaz en español, adaptable a escritorio y celular. Colores, espacios y tamaños compartidos mediante variables CSS.
- Login real separado de una vista previa explícita, de solo lectura y con datos ficticios.
- API centralizada en `js/api.js`; las pantallas no gestionan contraseñas ni almacenan tokens.
- Estados de carga, ausencia de datos, error, sesión vencida y conflicto de edición con mensajes visibles.
- Motivos, referencias y revisiones se envían cuando corresponde. El backend verifica las reglas y la autorización.
- Documentación propia del frontend. El backend no se modificó para este trabajo.

## Cómo se comunican las partes

1. El usuario completa un formulario HTML.
2. JavaScript toma los datos y construye el objeto de petición definido por el backend.
3. `api.js` lo envía como JSON mediante `fetch`, con JWT cuando corresponde.
4. La API valida permisos y reglas, coordina sus capas y consulta o guarda en PostgreSQL.
5. El frontend presenta la respuesta o explica el error. Nunca se conecta directamente a PostgreSQL.

La carpeta `front` no replica Domain, Application, Infrastructure y Contracts: esa separación pertenece al backend. Aquí se separan estilos, pantallas, acciones y comunicación HTTP.

## Convención CSS: BEM

Un bloque representa un componente; un elemento pertenece al bloque; un modificador cambia su variante.

```css
.tarjeta { }
.tarjeta__titulo { }
.boton { }
.boton--secundario { }
```

Usar nombres propios en español, minúsculas, sin tildes y con guiones. No nombrar por posición accidental o color, como `caja-izquierda-verde`. Reutilizar los componentes y las variables existentes antes de agregar estilos. Mantener los estilos en CSS y las acciones en JavaScript.

## Comprobaciones realizadas

- Cinco pruebas automatizadas de la capa HTTP, precisión decimal y escape de texto: aprobadas.
- Navegador Edge: recorrido de 14 rutas de vista previa, búsqueda, menú móvil, ausencia de desbordamiento horizontal a 390 px y guardado deshabilitado en vista previa.
- Capturas revisadas en escritorio de 1440 px y celular de 390 px.
- Navegador con API simulada según los contratos actuales: login, JWT, ausencia de token persistido, texto externo escapado, conflicto HTTP 409, distancia decimal exacta, cierre ante HTTP 401 y vista previa sin peticiones a la API.

**La integración con una cuenta real y PostgreSQL no se ejecutó en esta entrega.** Las simulaciones permiten comprobar el comportamiento del frontend, pero no sustituyen esa prueba conjunta. Tampoco se ejecutaron aquí migraciones ni se alteraron datos reales.

## Pendientes y límites de esta versión

- Probar con la API local encendida, cuenta real y permisos de Administrativo, Directivo y Root. Verificar rechazo de operaciones por el servidor.
- Integrar Vacantes y Secciones; las páginas actuales indican expresamente que están pendientes.
- Agregar administración de usuarios y recuperación/cambio de contraseña cuando se acuerden sus pantallas y operaciones. El acceso actual usa cuentas existentes.
- Agregar edición de oferta existente, consulta de publicaciones anteriores e historial administrativo completo. La ficha muestra historial de revisiones de domicilio y distancias.
- La corrección de solicitud conserva las alternativas ya aceptadas; aún no hay edición de esas alternativas ni corrección de datos personales compartidos del aspirante.
- Reutilizar aspirantes y responsables por identificador. Incorporar buscadores específicos cuando sus operaciones estén disponibles; por ahora, conservar el identificador devuelto al registrar un aspirante si se interrumpe la carga.
- Aspirante/solicitud, creación/vinculación de responsable y publicación/asignación de versiones son operaciones separadas. Si falla un paso posterior, el anterior puede haberse guardado. Los formularios conservan los identificadores durante el intento e informan el caso; no se presenta como una transacción única.
- Duplicados muestra hasta 100 alertas y avisa si hay más. Falta paginar esa pantalla. No fusiona ni elimina solicitudes.
- La distancia es manual y provisoria. Geoapify queda pendiente, al igual que las reglas todavía pendientes en el backend; este frontend no las considera implementadas.
- Confirmar publicación, HTTPS y CORS. La sesión en memoria implica volver a ingresar al recargar o cerrar la página.

Esta versión permite comenzar la prueba visual y funcional de Configuración e Inscripciones; no significa que todas las historias del sistema estén terminadas.
