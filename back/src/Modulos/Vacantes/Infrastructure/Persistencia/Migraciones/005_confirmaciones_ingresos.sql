CREATE SCHEMA IF NOT EXISTS vacantes;

ALTER TABLE vacantes.propuestas_ingresos
    ADD COLUMN IF NOT EXISTS "PropuestaAnteriorId" uuid;

CREATE TABLE IF NOT EXISTS vacantes.aceptaciones_turno (
    "Id" uuid PRIMARY KEY,
    "PeriodoId" uuid NOT NULL,
    "PropuestaId" uuid NOT NULL,
    "SolicitudId" uuid NOT NULL,
    "TurnoId" uuid NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "Referencia" character varying(250)
);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_aceptaciones_turno_propuesta_solicitud_turno"
    ON vacantes.aceptaciones_turno ("PropuestaId", "SolicitudId", "TurnoId");

CREATE TABLE IF NOT EXISTS vacantes.ingresos_confirmados (
    "Id" uuid PRIMARY KEY,
    "PeriodoId" uuid NOT NULL,
    "SolicitudId" uuid NOT NULL,
    "AspiranteId" uuid NOT NULL,
    "OfertaId" uuid NOT NULL,
    "TurnoId" uuid NOT NULL,
    "Sala" integer NOT NULL,
    "PropuestaId" uuid NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "Revision" integer NOT NULL,
    CONSTRAINT "CK_ingresos_confirmados_sala" CHECK ("Sala" BETWEEN 3 AND 5)
);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_ingresos_confirmados_periodo_aspirante"
    ON vacantes.ingresos_confirmados ("PeriodoId", "AspiranteId");
