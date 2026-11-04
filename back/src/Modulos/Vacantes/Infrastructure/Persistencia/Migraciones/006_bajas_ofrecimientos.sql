-- Aplicar después de 005. No modifica ingresos históricos ni cancela reservas existentes.
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "Activo" boolean NOT NULL DEFAULT true;
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "BajaEnProceso" boolean NOT NULL DEFAULT false;
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "UsuarioBajaId" uuid;
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "FechaSolicitudBaja" timestamp with time zone;
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "FechaBaja" timestamp with time zone;
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "MotivoBaja" text;
ALTER TABLE vacantes.ingresos_confirmados ADD COLUMN IF NOT EXISTS "OfrecimientoId" uuid;
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ingresos_confirmados_OfrecimientoId"
 ON vacantes.ingresos_confirmados ("OfrecimientoId") WHERE "OfrecimientoId" IS NOT NULL;

CREATE TABLE IF NOT EXISTS vacantes.ofrecimientos (
 "Id" uuid PRIMARY KEY,
 "OperacionId" uuid NOT NULL,
 "PeriodoId" uuid NOT NULL,
 "InstitucionId" uuid NOT NULL,
 "SolicitudId" uuid NOT NULL,
 "AspiranteId" uuid NOT NULL,
 "OfertaId" uuid NOT NULL,
 "TurnoId" uuid NOT NULL,
 "PropuestaListaId" uuid NOT NULL,
 "ReservaId" uuid NOT NULL,
 "UsuarioId" uuid NOT NULL,
 "Fecha" timestamp with time zone NOT NULL,
 "Motivo" text NOT NULL,
 "Estado" text NOT NULL,
 "Revision" integer NOT NULL,
 "UsuarioRespuestaId" uuid,
 "FechaRespuesta" timestamp with time zone,
 "MotivoRespuesta" text,
 "ReferenciaRespuesta" text,
 "IngresoId" uuid,
 CONSTRAINT "CK_ofrecimientos_estado" CHECK ("Estado" IN ('Ofrecido','Aceptado','Rechazado','Cancelado','Confirmado'))
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ofrecimientos_PeriodoId_OperacionId"
 ON vacantes.ofrecimientos ("PeriodoId", "OperacionId");
CREATE TABLE IF NOT EXISTS vacantes.cambios_ofrecimientos (
 "Id" uuid PRIMARY KEY,
 "OfrecimientoId" uuid NOT NULL,
 "UsuarioId" uuid NOT NULL,
 "Fecha" timestamp with time zone NOT NULL,
 "EstadoAnterior" text NOT NULL,
 "EstadoNuevo" text NOT NULL,
 "Motivo" text NOT NULL,
 "Referencia" text
);
CREATE INDEX IF NOT EXISTS "IX_cambios_ofrecimientos_OfrecimientoId"
 ON vacantes.cambios_ofrecimientos ("OfrecimientoId");
