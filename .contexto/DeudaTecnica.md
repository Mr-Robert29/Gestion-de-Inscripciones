# Deuda técnica

## HU-15 — Dependencia de HU-13

### DT-HU15-01 — Validar el momento funcional del registro del sorteo

HU-15 debe registrar sorteos dentro del flujo de procesamiento posterior al cierre de recepción definido por HU-13. La implementación inicial valida el período, la institución, el usuario y los permisos, pero deja pendiente exigir el estado funcional exacto de HU-13 hasta que el contrato definitivo de cierre y preparación del procesamiento esté integrado en Vacantes.

La deuda se resolverá incorporando una consulta pública de estado de procesamiento del período, sin acceder al contexto de Configuración desde Vacantes. Al resolverla se deberá:

- impedir registrar un sorteo si la recepción todavía no fue cerrada;
- conservar la autorización institucional vigente;
- mantener la consulta de sorteos históricos y vigentes;
- agregar comprobaciones de integración para período preparado, recepción abierta y recepción cerrada.

Esta es la única deuda técnica de HU-15 relacionada con HU-13. No se incorporan en este archivo pendientes de HU-16, HU-17 ni de otras historias.
