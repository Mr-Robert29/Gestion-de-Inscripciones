CREATE SCHEMA IF NOT EXISTS vacantes;

CREATE TABLE IF NOT EXISTS vacantes.casos_revision (
    "Id" uuid NOT NULL,
    "PeriodoId" uuid NOT NULL,
    "Sala" integer NOT NULL,
    "Tipo" text NOT NULL,
    "Estado" text NOT NULL,
    "SolicitudesAfectadas" jsonb NOT NULL,
    "OrdenDecidido" jsonb NOT NULL,
    "HuellaOrigen" character varying(500) NOT NULL,
    "SorteoId" uuid,
    "VersionSorteo" integer,
    "VersionReglasId" uuid,
    "UsuarioResolucion" uuid,
    "FechaResolucion" timestamp with time zone,
    "MotivoResolucion" character varying(1000),
    "FechaCreacion" timestamp with time zone NOT NULL,
    "Revision" integer NOT NULL,
    CONSTRAINT "PK_casos_revision" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_casos_revision_origen"
    ON vacantes.casos_revision ("PeriodoId", "Sala", "Tipo", "HuellaOrigen");

CREATE INDEX IF NOT EXISTS "IX_casos_revision_periodo_estado"
    ON vacantes.casos_revision ("PeriodoId", "Estado");

CREATE TABLE IF NOT EXISTS vacantes.historial_casos_revision (
    "Id" uuid NOT NULL,
    "CasoRevisionId" uuid NOT NULL,
    "Accion" character varying(100) NOT NULL,
    "EstadoAnterior" text NOT NULL,
    "EstadoNuevo" text NOT NULL,
    "OrdenAnterior" jsonb NOT NULL,
    "OrdenNueva" jsonb NOT NULL,
    "Motivo" character varying(1000) NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_historial_casos_revision" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_historial_casos_revision_casos_revision_CasoRevisionId"
        FOREIGN KEY ("CasoRevisionId") REFERENCES vacantes.casos_revision ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_historial_casos_revision_caso_fecha"
    ON vacantes.historial_casos_revision ("CasoRevisionId", "Fecha");
