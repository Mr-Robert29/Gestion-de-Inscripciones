CREATE SCHEMA IF NOT EXISTS vacantes;

CREATE TABLE IF NOT EXISTS vacantes.sorteos
(
    id uuid NOT NULL PRIMARY KEY,
    periodo_id uuid NOT NULL,
    sala integer NOT NULL,
    numero integer NOT NULL,
    fecha date NOT NULL,
    referencia varchar(250) NOT NULL,
    version integer NOT NULL,
    vigente boolean NOT NULL,
    usuario_id uuid NOT NULL,
    fecha_registro timestamp with time zone NOT NULL,
    CONSTRAINT ck_sorteo_numero CHECK (numero BETWEEN 0 AND 999),
    CONSTRAINT ck_sorteo_sala CHECK (sala BETWEEN 3 AND 5),
    CONSTRAINT uq_sorteo_version UNIQUE (periodo_id, sala, version)
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_sorteo_vigente
ON vacantes.sorteos(periodo_id, sala)
WHERE vigente = TRUE;

CREATE INDEX IF NOT EXISTS ix_sorteo_periodo
ON vacantes.sorteos(periodo_id);
