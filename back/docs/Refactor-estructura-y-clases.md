# Estructura y clases de Domain

11 de octubre de 2026. Alcance corregido a pedido del usuario: **estructura de proyectos y adaptación de clases de Domain**. El equipo desarrollará las demás capas. No se ejecutaron comandos de Git ni operaciones en GitHub.

## Qué contiene esta entrega

La solución `Jardin.sln` está en la raíz. Tiene veinte bibliotecas de módulos y una API con arranque mínimo. Cada módulo tiene proyectos Domain, Application, Infrastructure y Contracts.

Application, Infrastructure y Contracts están vacíos: solo contienen sus archivos de proyecto y las referencias entre capas. La API no tiene operaciones, autenticación ni configuración de base de datos.

## Destino de las clases anteriores

| Clase o concepto anterior | Ubicación actual | Qué se adaptó |
|---|---|---|
| Alumno | Inscripciones/Domain/Entidades/Aspirante.cs | Nombre del concepto, documento opcional y edad calculada con fecha de corte explícita. |
| Responsable | Inscripciones/Domain/Entidades/Responsable.cs | Datos personales y contacto de una persona reutilizable. |
| Vínculo y tutoría dentro de Responsable | Inscripciones/Domain/Entidades/VinculoResponsable.cs | Se expresan por relación con cada aspirante. |
| Domicilio | Inscripciones/Domain/ObjetosDeValor/Domicilio.cs | Dirección separada de las coordenadas, preparada para la futura solicitud. |
| Ubicación | Inscripciones/Domain/ObjetosDeValor/Coordenadas.cs | Latitud y longitud con validación de sus rangos. |
| Dirección y ubicación del jardín | Configuracion/Domain/ObjetosDeValor | Tipos propios del módulo, preparados para la futura entidad del establecimiento. |
| Usuario abstracto | Usuarios/Domain/Entidades/UsuarioPersonal.cs | Perfil, nombre, correo y estado activo. |
| Administrativo, Director y Maestro | Usuarios/Domain/RolesPersonal.cs | Nombres de los roles aprobados: Administrativo, Directivo y AdministradorDelSistema. No aplica permisos todavía. |

Los repositorios, contextos, comandos, consultas y controladores anteriores no forman parte de esta base reducida. Tampoco se conservan aquí sus reemplazos implementados en la intervención anterior. Las responsabilidades correspondientes se desarrollarán posteriormente con el equipo.

El código del proyecto antiguo ya había sido retirado en la intervención anterior. Esta reducción no restaura ese proyecto. La carpeta `src/Gestion-de-Inscripciones` conserva únicamente dos archivos ocultos `.suo` de Visual Studio, que no participan de la solución. Su eliminación forzada fue rechazada previamente por la revisión automática y queda para el usuario desde el Explorador.

## Qué se retiró para respetar el alcance

Se retiraron los servicios de Application, interfaces y DTO de Contracts, repositorios, contextos y migraciones de Infrastructure. También se retiraron Identity, JWT, auditoría, permisos HTTP, controladores, Swagger, CORS, Docker y el ejecutable de verificaciones de la intervención anterior. Se quitaron los paquetes externos que se habían agregado para esas implementaciones.

Antes de reducir se guardó una copia del código de la intervención anterior fuera del repositorio, en `C:/Users/PcHome/AppData/Local/Temp/Inscripciones-antes-reduccion-20261011-234850`. Esa copia no contiene el código original que ya se había eliminado y no forma parte de la entrega.

## Estado de las decisiones

**Confirmado:** estructura modular con cuatro proyectos por módulo, nombres propios en español, concepto Aspirante y decisiones funcionales P-01 a P-08 acordadas en la conversación. Root tendrá acceso a todas las operaciones. Geoapify resolverá ubicación y distancia, sin Haversine.

**Implementado:** estructura, arranque mínimo de API y clases de dominio de la tabla. Se conservaron identificadores Guid y fechas de nacimiento DateOnly de la adaptación anterior; son elecciones técnicas de esta base, no nuevos requisitos funcionales.

**Pendiente:** modelo de dominio restante, revisión detallada de atributos y reglas con el equipo, casos de uso, contratos, persistencia, seguridad, controladores, Swagger, Geoapify y pruebas de comportamiento. Las reglas completas de obligatoriedad de domicilio siguen pendientes. No se generó ni modificó una base de datos.

**Comprobado:** recompilación de Jardin.sln con 0 errores y 0 advertencias; 21 proyectos con referencias válidas, nueve clases y tipos de Domain conservados sin cambios respecto de la copia previa, y ninguna implementación en las otras capas ni paquetes externos agregados. Las 24 verificaciones de la intervención anterior ya no describen esta entrega, porque su proyecto y las implementaciones que ejercitaban fueron retirados.

## Registro de aprendizaje

- **Domain:** una clase representa un concepto del negocio; sus métodos expresan comportamientos de ese concepto. Una persona responsable y su vínculo con un aspirante tienen responsabilidades distintas.
- **Application:** coordinará un caso de uso, como registrar un aspirante. Tener la entidad todavía no permite registrarla mediante HTTP ni guardarla.
- **Infrastructure:** implementará el acceso a PostgreSQL y a Geoapify, así como Identity y JWT. Domain no depende de esas herramientas.
- **Contracts:** definirá interfaces y datos públicos cuando exista una colaboración concreta que los necesite.

Una solución agrupa proyectos. Cada proyecto compila una parte del código. Las carpetas organizan los archivos dentro de esos proyectos. La estructura está preparada; el desarrollo funcional sigue a cargo del equipo.

