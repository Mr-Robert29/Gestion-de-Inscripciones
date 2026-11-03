# Gestión de inscripciones de jardines

Backend C#/.NET 10 organizado como monolito modular. El frontend vive en ../front y tendrá su propio stack y documentación.

## Estructura

Desde esta carpeta back, Jardin.sln agrupa la API, los veinte proyectos de los cinco módulos y el ejecutable de comprobaciones de Configuración.

```text
back/
  Jardin.sln
  src/
    Jardin.Api/
    Modulos/
      Usuarios/
      Configuracion/
      Inscripciones/
      Vacantes/
      Secciones/
  pruebas/
    Jardin.Configuracion.Pruebas/
  docs/
```

Cada módulo tiene Domain, Application, Infrastructure y Contracts.

- **Domain:** conceptos y reglas del negocio.
- **Application:** coordinación de casos de uso.
- **Infrastructure:** persistencia y servicios externos.
- **Contracts:** operaciones y datos públicos del módulo.
- **API:** peticiones y respuestas HTTP.

## Estado actual

Usuarios dispone del trabajo de cuentas, Identity y JWT desarrollado por el compañero. Configuración incorpora registro, consulta, actualización, permisos, historial y migración propia. Inscripciones conserva sus clases iniciales de dominio; los procesos de mediciones y propuestas aún están pendientes.

La detección de referencias afectadas por cambios de ubicación está preparada, pero la integración con mediciones y confirmaciones reales no puede declararse terminada hasta construir sus procesos de origen.

El estado detallado y los comandos de uso están en [HU-01 — Implementación](docs/HU-01-Implementacion.md). Los documentos de continuidad anteriores conservan el contexto histórico de cada fecha.

## Compilar y comprobar

```powershell
dotnet build Jardin.sln
dotnet run --project pruebas/Jardin.Configuracion.Pruebas
```

Se necesita el SDK de .NET 10. La suite utiliza SQLite aislado por defecto. El documento de HU-01 explica cómo ejecutar también las comprobaciones con PostgreSQL exclusivo de pruebas.

## Configuración local

La API requiere Jwt__ClaveSecreta y la conexión de Usuarios existente. Configuración admite ConnectionStrings__Configuracion y, si se omite, utiliza DefaultConnection de la API. No subir contraseñas, claves JWT ni tokens al repositorio.

Antes de arrancar contra una base local, aplicar la migración de Configuración siguiendo el documento de HU-01. Las pruebas no requieren modificar las bases de la aplicación.

Geoapify sigue siendo el proveedor aprobado de coordenadas y distancia para la integración posterior. No se implementó Haversine.

## Primera versión de HU-02

Configuración también permite crear y consultar períodos y abrir/cerrar recepción manualmente. Se pospuso por decisión del usuario la validación de oferta/reglas al abrir.

- [HU-02 — Primera versión](docs/HU-02-Primera-version.md).
- [Cosas pendientes de las historias trabajadas](<docs/Cosas pendientes.md>).
