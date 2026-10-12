# Gestión de inscripciones de jardines

Base de trabajo para un backend en C#/.NET 10 organizado como monolito modular. Esta entrega contiene **la estructura y las clases de Domain**. Las funcionalidades restantes se desarrollarán con el equipo, paso a paso.

## Estructura

La solución `Jardin.sln` está en la raíz. Agrupa 21 proyectos: la API y cuatro proyectos por cada uno de los cinco módulos.

```text
Jardin.sln
src/
  Jardin.Api/                  Arranque mínimo, sin operaciones HTTP
  Modulos/
    Usuarios/
      Domain/                  UsuarioPersonal y RolesPersonal
      Application/             Proyecto vacío
      Infrastructure/          Proyecto vacío
      Contracts/               Proyecto vacío
    Configuracion/
      Domain/                  Domicilio y Coordenadas del establecimiento
      Application/             Proyecto vacío
      Infrastructure/          Proyecto vacío
      Contracts/               Proyecto vacío
    Inscripciones/
      Domain/                  Aspirante, Responsable, VinculoResponsable,
                               Domicilio y Coordenadas
      Application/             Proyecto vacío
      Infrastructure/          Proyecto vacío
      Contracts/               Proyecto vacío
    Vacantes/
      Domain/                  Proyecto vacío
      Application/             Proyecto vacío
      Infrastructure/          Proyecto vacío
      Contracts/               Proyecto vacío
    Secciones/
      Domain/                  Proyecto vacío
      Application/             Proyecto vacío
      Infrastructure/          Proyecto vacío
      Contracts/               Proyecto vacío
docs/
  Refactor-estructura-y-clases.md
```

Los proyectos vacíos conservan sus referencias para establecer la dirección de las dependencias. Todavía no contienen servicios, repositorios, contratos públicos ni persistencia.

## Responsabilidad de las capas

- **Domain:** conceptos y reglas del negocio expresados con clases y métodos; sin acceso a bases de datos ni servicios externos.
- **Application:** futuros casos de uso y coordinación de las operaciones.
- **Infrastructure:** futuras implementaciones de persistencia y servicios externos.
- **Contracts:** futuras interfaces y datos públicos para colaborar entre módulos.

`Jardin.Api` será el punto de entrada HTTP. Por ahora únicamente puede arrancar; no tiene controladores ni rutas de negocio.

## Clases conservadas

`Aspirante` reemplaza a `Alumno`. Tiene datos personales, documento opcional, cálculo de edad a una fecha de corte explícita y vínculos con responsables. `Responsable` representa una persona; `VinculoResponsable` guarda su relación con un aspirante, incluyendo tipo de vínculo, contacto principal y tutoría.

`UsuarioPersonal` representa el perfil y su estado activo. `RolesPersonal` conserva los nombres Administrativo, Directivo y AdministradorDelSistema. Los nombres de roles todavía no aplican permisos: esa implementación queda pendiente.

`Domicilio` representa una dirección y `Coordenadas` una ubicación. Cada módulo conserva sus propios tipos. En Inscripciones el domicilio está preparado para la futura solicitud, que todavía no existe; no está asociado al registro global de Aspirante. En Configuracion está preparado para el establecimiento, cuya entidad también queda pendiente.

Estas clases son una base inicial de dominio, no el modelo completo del sistema.

## Compilar

Desde la raíz:

```powershell
dotnet build Jardin.sln
```

Se requiere el SDK de .NET 10. La comprobación de esta entrega es de compilación; no demuestra funcionamiento de procesos de inscripción ni integración con una base de datos.

## Trabajo posterior

El stack acordado incluye ASP.NET Core Web API, Swagger, PostgreSQL, EF Core con Npgsql, Identity y JWT. En esta base solo está preparado el proyecto de ASP.NET Core; Swagger, persistencia y autenticación se implementarán después. No hay paquetes adicionales para esas funcionalidades, migraciones, conexiones, login ni configuración de Docker.

También quedan pendientes los casos de uso, los contratos públicos, los controladores, las reglas restantes de dominio y la integración con Geoapify. Se conserva la decisión de utilizar Geoapify para ubicación y distancia, sin implementar Haversine.

Las decisiones funcionales aprobadas mantienen su validez; aprobación e implementación son estados distintos. La documentación funcional del equipo continúa enlazada en `readme.txt`. El registro de alcance y aprendizaje está en [docs/Refactor-estructura-y-clases.md](docs/Refactor-estructura-y-clases.md).

Comprobación de esta base: 21 proyectos compilados con 0 errores y 0 advertencias.
