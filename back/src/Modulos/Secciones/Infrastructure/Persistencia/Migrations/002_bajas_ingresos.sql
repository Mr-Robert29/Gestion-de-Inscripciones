CREATE SCHEMA IF NOT EXISTS secciones;
CREATE TABLE IF NOT EXISTS secciones.ingresos_retirados (
 "IngresoId" uuid PRIMARY KEY,
 "PeriodoId" uuid NOT NULL,
 "UsuarioId" uuid NOT NULL,
 "Fecha" timestamp with time zone NOT NULL,
 "Motivo" text NOT NULL
);
CREATE TABLE IF NOT EXISTS secciones.cambios_baja_asignaciones (
 "Id" uuid PRIMARY KEY,
 "AsignacionId" uuid NOT NULL,
 "IngresoId" uuid NOT NULL,
 "SeccionId" uuid NOT NULL,
 "FechaAnterior" timestamp with time zone NOT NULL,
 "UsuarioAnteriorId" uuid NOT NULL,
 "MotivoAnterior" text,
 "EstadoAnterior" text NOT NULL,
 "UsuarioBajaId" uuid NOT NULL,
 "FechaBaja" timestamp with time zone NOT NULL,
 "MotivoBaja" text NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_cambios_baja_asignaciones_IngresoId"
 ON secciones.cambios_baja_asignaciones ("IngresoId");
