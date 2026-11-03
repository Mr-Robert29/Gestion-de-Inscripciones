CREATE SCHEMA IF NOT EXISTS vacantes;

CREATE TABLE IF NOT EXISTS vacantes.evaluaciones_prioridad
(
    id uuid NOT NULL PRIMARY KEY,
    periodo_id uuid NOT NULL,
    version_reglas_id uuid NOT NULL,
    usuario_id uuid NOT NULL,
    revision_periodo integer NOT NULL,
    fecha timestamp with time zone NOT NULL,
    datos jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_evaluacion_prioridad_periodo_fecha
ON vacantes.evaluaciones_prioridad(periodo_id, fecha);
