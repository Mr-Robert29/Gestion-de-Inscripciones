CREATE SCHEMA IF NOT EXISTS secciones;

CREATE TABLE IF NOT EXISTS secciones.propuestas_distribucion
(
    id uuid NOT NULL PRIMARY KEY,
    periodo_id uuid NOT NULL,
    version integer NOT NULL,
    revision_ingresos integer NOT NULL,
    revision_secciones integer NOT NULL,
    estado varchar(30) NOT NULL,
    fecha_creacion timestamp with time zone NOT NULL,
    creada_por uuid NOT NULL,
    confirmada_por uuid NULL,
    fecha_confirmacion timestamp with time zone NULL,
    CONSTRAINT uq_propuesta_periodo_version UNIQUE (periodo_id, version)
);

CREATE TABLE IF NOT EXISTS secciones.asignaciones_propuesta
(
    id uuid NOT NULL PRIMARY KEY,
    propuesta_distribucion_id uuid NOT NULL REFERENCES secciones.propuestas_distribucion(id) ON DELETE CASCADE,
    ingreso_id uuid NOT NULL,
    seccion_id uuid NULL,
    edad integer NOT NULL,
    turno_id uuid NOT NULL,
    motivo_sin_seccion varchar(500) NULL,
    modificada_por uuid NULL,
    modificada_en timestamp with time zone NULL,
    motivo_modificacion varchar(500) NULL,
    CONSTRAINT uq_propuesta_ingreso UNIQUE (propuesta_distribucion_id, ingreso_id)
);

CREATE TABLE IF NOT EXISTS secciones.asignaciones_seccion
(
    id uuid NOT NULL PRIMARY KEY,
    ingreso_id uuid NOT NULL,
    seccion_id uuid NOT NULL,
    estado varchar(20) NOT NULL,
    fecha timestamp with time zone NOT NULL,
    usuario_id uuid NOT NULL,
    motivo varchar(500) NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_asignacion_ingreso_activa
ON secciones.asignaciones_seccion(ingreso_id)
WHERE estado = 'Activa';
