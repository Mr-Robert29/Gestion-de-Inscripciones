> Documento histórico del 27 de octubre de 2026. Para el código actual y las integraciones pendientes, leer [HU-01-Implementacion.md](HU-01-Implementacion.md). Las referencias al guardado pendiente describen el estado de esa fecha.

# HU-01: primer recorrido de registro de una institución

Este documento explica el incremento de Contracts y Application. Se conserva Domain sin cambios. No se implementaron persistencia real, controladores, autorización ni historial; HU-01 continúa en desarrollo.

## Responsabilidades de las capas

- **Domain:** Institucion, Domicilio y Coordenadas representan conceptos y reglas locales.
- **Contracts:** publica los datos de entrada/salida y la operación disponible.
- **Application:** recibe la petición, construye los objetos del negocio y solicita guardarlos.
- **Infrastructure:** implementará ese guardado en PostgreSQL.

La API se incorporará después para recibir peticiones HTTP y llamar al caso de uso. Una petición de Contracts también puede utilizarse en una llamada normal de C#: no necesita HTTP para existir.

## Archivos y propósito

| Archivo dentro de Configuracion | Propósito |
|---|---|
| Contracts/DatosDomicilio.cs | Transportar los campos de la dirección, incluidos los adicionales opcionales. |
| Contracts/DatosCoordenadas.cs | Transportar latitud y longitud. |
| Contracts/RegistrarInstitucionPeticion.cs | Reunir todos los datos de entrada al registro. |
| Contracts/RegistrarInstitucionRespuesta.cs | Devolver únicamente el Id generado después de completar el guardado. No es una consulta de la institución. |
| Contracts/IModuloConfiguracion.cs | Declarar la operación pública RegistrarInstitucionAsync. |
| Application/Servicios/ServicioConfiguracion.cs | Implementar esa operación y coordinar la construcción y solicitud de guardado. |
| Application/Persistencia/IRepositorioInstituciones.cs | Declarar el guardado que necesita Application. Infrastructure lo implementará. |

La interfaz del repositorio es pública para que Infrastructure pueda implementarla desde su proyecto. Es una necesidad interna de Application, no el contrato público de negocio de otros módulos.

## Convención de nombres de Contracts

- **...Peticion:** datos de entrada a una operación, como RegistrarInstitucionPeticion.
- **...Respuesta:** resultado de salida de una operación, como RegistrarInstitucionRespuesta.
- **Datos...:** información agrupada que puede formar parte de peticiones o respuestas, como DatosDomicilio y DatosCoordenadas. Actualmente estos dos se usan en la entrada; el nombre permite reutilizarlos si una salida futura necesita la misma estructura.

La dirección se interpreta desde Configuración: la petición entra al módulo y la respuesta sale hacia quien lo llamó. Los DTO no se envían solos; se entregan como parámetros y se devuelven como resultados.

Se renombró DatosInstitucionRegistrada a RegistrarInstitucionRespuesta, sin cambiar su contenido ni el comportamiento del registro. Los comentarios de propósito se mantienen breves y en español.
## Recorrido de los datos

1. Quien llama entrega RegistrarInstitucionPeticion.
2. ServicioConfiguracion comprueba que existan la petición y sus datos de domicilio.
3. Convierte DatosDomicilio al Domicilio de Domain.
4. Si recibió DatosCoordenadas, construye Coordenadas de Domain, que comprueba sus rangos. Si no recibió coordenadas, mantiene null.
5. Construye Institucion. Se ejecutan las validaciones y normalizaciones del constructor existente.
6. Solicita AgregarAsync al repositorio y espera su finalización.
7. Devuelve RegistrarInstitucionRespuesta con el Id de la institución.

Los DTO no sustituyen los objetos de Domain: describen los datos públicos. Application realiza la conversión. Contracts no referencia Domain, EF Core ni PostgreSQL.

## Ejemplo de entrada

```csharp
var direccion = new DatosDomicilio(
    Calle: "San Martin",
    Numero: "123",
    Localidad: "La Plata",
    Provincia: "Buenos Aires");

var peticion = new RegistrarInstitucionPeticion(
    Nombre: "Jardin de prueba",
    Domicilio: direccion,
    TelefonoContacto: "12345");
```

Este ejemplo construye datos en memoria. No guarda nada por sí solo. Todavía no hay una implementación real del repositorio ni un registro del servicio en la API.

## Cómo utiliza la interfaz el servicio

ServicioConfiguracion recibe IRepositorioInstituciones en su constructor y conserva esa referencia en un campo privado. Recibir desde afuera el componente que necesita es inyección de dependencias por constructor.

Application conoce qué operación debe pedir, pero no conoce los detalles del guardado. La implementación de Infrastructure deberá completar el guardado antes de que AgregarAsync termine correctamente. Si falla, la excepción se propaga; el servicio no devuelve un resultado de éxito.

## Sintaxis utilizada

- **DTO:** objeto que transporta datos de entrada o salida. No ejecuta persistencia.
- **sealed record:** forma breve de declarar los DTO, siguiendo la convención ya utilizada en Usuarios.Contracts. Los parámetros de su declaración generan propiedades públicas con inicialización, no setters públicos ordinarios. También aporta igualdad por valores e impide herencia mediante sealed.
- **Task<RegistrarInstitucionRespuesta>:** operación que finalizará con ese resultado, o con un fallo/cancelación.
- **async / await:** permiten esperar la operación del repositorio sin bloquear el hilo durante una espera asíncrona. No crean por sí solos un hilo nuevo.
- **CancellationToken:** señal para solicitar la cancelación del trabajo. Se comprueba al comenzar y se transmite al repositorio; no equivale a una reversión automática del guardado.
- **string? / DatosCoordenadas?:** permiten ausencia de valor, expresada como null.

## Qué no está resuelto por este incremento

No se agregó consulta, actualización, base de datos, migraciones, API, Geoapify, autorización ni historial. Tampoco se aplicó una restricción nueva de cantidad de instituciones.

Se conservaron las validaciones de Domain: por ejemplo, el teléfono rechaza null pero aún acepta texto vacío, y el domicilio no comprueba todos los campos obligatorios. La obligatoriedad definitiva sigue pendiente; crear un DTO no aprueba nuevas reglas de negocio.

Antes de habilitar la operación desde la API deben integrarse los permisos y el contexto de usuario. El cambio de domicilio/ubicación con historial y revisión de mediciones corresponde a pasos posteriores de HU-01. Los módulos de Usuarios y Secciones no se modificaron.

## Estado y comprobaciones

- **Confirmado:** separación de capas y clases Domain trabajadas previamente.
- **Implementado:** DTO, contrato público y coordinación del registro mediante interfaz de persistencia.
- **Probado:** siete escenarios con un repositorio de prueba en un ejecutable temporal fuera del repositorio: entrega del objeto y su identificador, datos completos y normalización, coordenadas opcionales, rechazo de datos inválidos antes de guardar, cancelación previa, transmisión de cancelación y propagación de un fallo de persistencia.
- **Compilación:** Jardin.sln compiló con cero errores y cero advertencias.
- **Pendiente:** implementación real del repositorio y pruebas con PostgreSQL, además de los puntos de integración anteriores.

Las comprobaciones temporales no representan una suite de pruebas incorporada a la solución. No se conectó ni modificó una base de datos.

## Por dónde leer

Primero leer RegistrarInstitucionPeticion: muestra qué entra. Después ServicioConfiguracion.RegistrarInstitucionAsync: muestra qué hacemos con esos datos. Luego IRepositorioInstituciones: muestra qué trabajo dejamos pendiente para Infrastructure. Finalmente RegistrarInstitucionRespuesta: muestra qué devolvemos.

Ejercicio pequeño: identificar qué línea construye un objeto de Domain y cuál solicita su guardado. Son dos momentos distintos.


