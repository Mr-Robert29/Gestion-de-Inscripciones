CREATE SCHEMA IF NOT EXISTS secciones;
CREATE TABLE IF NOT EXISTS secciones.cambios_composicion (
    "Id" uuid PRIMARY KEY,
    "SeccionId" uuid NOT NULL,
    "PeriodoId" uuid NOT NULL,
    "EdadesAnteriores" text NOT NULL,
    "EdadesNuevas" text NOT NULL,
    "RevisionAnterior" integer NOT NULL,
    "RevisionNueva" integer NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "Motivo" text NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_cambios_composicion_SeccionId_RevisionNueva"
    ON secciones.cambios_composicion ("SeccionId", "RevisionNueva");
