-- Aplicar después de 001, 002 y 003. HU-18 no aplica scripts al arrancar.
CREATE SCHEMA IF NOT EXISTS vacantes;
CREATE TABLE IF NOT EXISTS vacantes.propuestas_ingresos (
    "Id" uuid PRIMARY KEY,
    "PeriodoId" uuid NOT NULL,
    "InstitucionId" uuid NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "Motivo" text NOT NULL,
    "HuellaEntradas" character varying(64) NOT NULL,
    "Entradas" jsonb NOT NULL,
    "Resultados" jsonb NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_propuestas_ingresos_PeriodoId_Fecha"
    ON vacantes.propuestas_ingresos ("PeriodoId", "Fecha");

-- HU-19 y HU-21 deben mantener este registro al confirmar, reservar o liberar.
-- Las ocupaciones iniciales publicadas son anteriores a estos movimientos y no se duplican aquí.
CREATE TABLE IF NOT EXISTS vacantes.ocupaciones_plazas (
    "Id" uuid PRIMARY KEY,
    "PeriodoId" uuid NOT NULL,
    "AspiranteId" uuid NOT NULL,
    "OfertaId" uuid NOT NULL,
    "Sala" integer NOT NULL,
    "Tipo" character varying(20) NOT NULL,
    "Activa" boolean NOT NULL,
    "Revision" integer NOT NULL,
    CONSTRAINT "CK_ocupaciones_plazas_sala" CHECK ("Sala" BETWEEN 3 AND 5),
    CONSTRAINT "CK_ocupaciones_plazas_tipo" CHECK ("Tipo" IN ('Ingreso', 'Reserva'))
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ocupaciones_plazas_PeriodoId_AspiranteId"
    ON vacantes.ocupaciones_plazas ("PeriodoId", "AspiranteId") WHERE "Activa" = true;
