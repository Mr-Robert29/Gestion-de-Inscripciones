# Decisiones de HU-15 — Registrar el número del sorteo

## Alcance

HU-15 registra el número informado del sorteo, la fecha, la referencia y el grupo participante. El sistema no genera números aleatorios ni decide quién participa.

## Decisiones aprobadas

1. La funcionalidad se implementa dentro del módulo Vacantes.
2. Se crea un contrato específico `IModuloSorteos`; no se agregan operaciones de sorteo al contrato existente de ingresos activos.
3. El grupo participante se representa mediante `Sala`, con valores válidos de 3, 4 o 5, en concordancia con el modelo actual de Inscripciones.
4. El número se almacena como entero entre 0 y 999 y se muestra siempre con tres dígitos.
5. Se expone `POST /api/v1/periodos/{periodoId}/sorteos` para registrar el sorteo.
6. Se expone `GET /api/v1/periodos/{periodoId}/sorteos` para consultar el registro vigente del período.
7. La identidad del usuario se obtiene desde la sesión autenticada; nunca se recibe como dato confiable en el cuerpo de la petición.
8. La autorización exige usuario activo, permiso `Admision.RegistrarSorteo` y acceso a la institución asociada al período.
9. Vacantes no consulta directamente los contextos de Configuración, Inscripciones o Usuarios. La coordinación se realiza mediante contratos y una abstracción de autorización implementada por el host.
10. Se conserva historial mediante versiones inmutables. Para una misma combinación de período y sala solo puede existir un registro vigente; un nuevo registro desactiva el anterior dentro de una transacción.
11. La consulta devuelve únicamente el registro vigente de cada sala del período y mantiene el número con formato de tres dígitos.
12. La migración utiliza el esquema propio `vacantes` y no modifica tablas de otros módulos.

## Resultado esperado

El registro queda disponible para que HU-16 calcule posteriormente el orden circular utilizando el número informado. HU-15 no ordena solicitudes, no resuelve empates y no confirma ingresos.
