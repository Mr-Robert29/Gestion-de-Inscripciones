DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'secciones') THEN
        CREATE SCHEMA secciones;
    END IF;
END $EF$;


CREATE TABLE secciones.asignaciones_seccion (
    "Id" uuid NOT NULL,
    "IngresoId" uuid NOT NULL,
    "SeccionId" uuid NOT NULL,
    "Estado" character varying(20) NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Motivo" character varying(500),
    CONSTRAINT "PK_asignaciones_seccion" PRIMARY KEY ("Id")
);


CREATE TABLE secciones.cambios_baja_asignaciones (
    "Id" uuid NOT NULL,
    "AsignacionId" uuid NOT NULL,
    "IngresoId" uuid NOT NULL,
    "SeccionId" uuid NOT NULL,
    "FechaAnterior" timestamp with time zone NOT NULL,
    "UsuarioAnteriorId" uuid NOT NULL,
    "MotivoAnterior" text,
    "EstadoAnterior" text NOT NULL,
    "UsuarioBajaId" uuid NOT NULL,
    "FechaBaja" timestamp with time zone NOT NULL,
    "MotivoBaja" text NOT NULL,
    CONSTRAINT "PK_cambios_baja_asignaciones" PRIMARY KEY ("Id")
);


CREATE TABLE secciones.cambios_composicion (
    "Id" uuid NOT NULL,
    "SeccionId" uuid NOT NULL,
    "PeriodoId" uuid NOT NULL,
    "EdadesAnteriores" text NOT NULL,
    "EdadesNuevas" text NOT NULL,
    "RevisionAnterior" integer NOT NULL,
    "RevisionNueva" integer NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "Motivo" text NOT NULL,
    CONSTRAINT "PK_cambios_composicion" PRIMARY KEY ("Id")
);


CREATE TABLE secciones.ingresos_retirados (
    "IngresoId" uuid NOT NULL,
    "PeriodoId" uuid NOT NULL,
    "UsuarioId" uuid NOT NULL,
    "Fecha" timestamp with time zone NOT NULL,
    "Motivo" text NOT NULL,
    CONSTRAINT "PK_ingresos_retirados" PRIMARY KEY ("IngresoId")
);


CREATE TABLE secciones.propuestas_distribucion (
    "Id" uuid NOT NULL,
    "PeriodoId" uuid NOT NULL,
    "Version" integer NOT NULL,
    "RevisionIngresos" integer NOT NULL,
    "RevisionSecciones" integer NOT NULL,
    "Estado" character varying(30) NOT NULL,
    "FechaCreacion" timestamp with time zone NOT NULL,
    "CreadaPor" uuid NOT NULL,
    "ConfirmadaPor" uuid,
    "FechaConfirmacion" timestamp with time zone,
    CONSTRAINT "PK_propuestas_distribucion" PRIMARY KEY ("Id")
);


CREATE TABLE secciones.secciones (
    "Id" uuid NOT NULL,
    "PeriodoId" uuid NOT NULL,
    "AulaId" uuid NOT NULL,
    "TurnoId" uuid NOT NULL,
    "Nombre" character varying(150) NOT NULL,
    "Tipo" character varying(20) NOT NULL,
    "Capacidad" integer NOT NULL,
    "Revision" integer NOT NULL,
    "Activa" boolean NOT NULL,
    edades_admitidas text NOT NULL,
    CONSTRAINT "PK_secciones" PRIMARY KEY ("Id")
);


CREATE TABLE secciones.asignaciones_propuesta (
    "Id" uuid NOT NULL,
    "PropuestaDistribucionId" uuid NOT NULL,
    "IngresoId" uuid NOT NULL,
    "SeccionId" uuid,
    "Edad" integer NOT NULL,
    "TurnoId" uuid NOT NULL,
    "MotivoSinSeccion" character varying(500),
    "ModificadaPor" uuid,
    "ModificadaEn" timestamp with time zone,
    "MotivoModificacion" character varying(500),
    CONSTRAINT "PK_asignaciones_propuesta" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_asignaciones_propuesta_propuestas_distribucion_PropuestaDis~" FOREIGN KEY ("PropuestaDistribucionId") REFERENCES secciones.propuestas_distribucion ("Id") ON DELETE CASCADE
);


CREATE UNIQUE INDEX "IX_asignaciones_propuesta_PropuestaDistribucionId_IngresoId" ON secciones.asignaciones_propuesta ("PropuestaDistribucionId", "IngresoId");


CREATE UNIQUE INDEX "IX_asignaciones_seccion_IngresoId" ON secciones.asignaciones_seccion ("IngresoId") WHERE "Estado" = 'Activa';


CREATE INDEX "IX_cambios_baja_asignaciones_IngresoId" ON secciones.cambios_baja_asignaciones ("IngresoId");


CREATE UNIQUE INDEX "IX_cambios_composicion_SeccionId_RevisionNueva" ON secciones.cambios_composicion ("SeccionId", "RevisionNueva");


CREATE UNIQUE INDEX "IX_propuestas_distribucion_PeriodoId_Version" ON secciones.propuestas_distribucion ("PeriodoId", "Version");


CREATE INDEX "IX_secciones_PeriodoId_AulaId_TurnoId" ON secciones.secciones ("PeriodoId", "AulaId", "TurnoId");


