DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'vacantes') THEN
        CREATE SCHEMA vacantes;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS vacantes."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'vacantes') THEN
            CREATE SCHEMA vacantes;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.aceptaciones_turno (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "PropuestaId" uuid NOT NULL,
        "SolicitudId" uuid NOT NULL,
        "TurnoId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "Referencia" character varying(250),
        CONSTRAINT "PK_aceptaciones_turno" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.cambios_ofrecimientos (
        "Id" uuid NOT NULL,
        "OfrecimientoId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "EstadoAnterior" text NOT NULL,
        "EstadoNuevo" text NOT NULL,
        "Motivo" text NOT NULL,
        "Referencia" text,
        CONSTRAINT "PK_cambios_ofrecimientos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.casos_revision (
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.evaluaciones_prioridad (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "VersionReglasId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "RevisionPeriodo" integer NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "Datos" jsonb NOT NULL,
        CONSTRAINT "PK_evaluaciones_prioridad" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.historial_casos_revision (
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
        CONSTRAINT "PK_historial_casos_revision" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.ingresos_confirmados (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "SolicitudId" uuid NOT NULL,
        "AspiranteId" uuid NOT NULL,
        "Edad" integer NOT NULL,
        "OfertaId" uuid NOT NULL,
        "TurnoId" uuid NOT NULL,
        "Sala" integer NOT NULL,
        "PropuestaId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "Revision" integer NOT NULL,
        "Activo" boolean NOT NULL,
        "BajaEnProceso" boolean NOT NULL,
        "UsuarioBajaId" uuid,
        "FechaSolicitudBaja" timestamp with time zone,
        "FechaBaja" timestamp with time zone,
        "MotivoBaja" text,
        "OfrecimientoId" uuid,
        CONSTRAINT "PK_ingresos_confirmados" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.ocupaciones_plazas (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "AspiranteId" uuid NOT NULL,
        "OfertaId" uuid NOT NULL,
        "Sala" integer NOT NULL,
        "Tipo" character varying(20) NOT NULL,
        "Activa" boolean NOT NULL,
        "Revision" integer NOT NULL,
        CONSTRAINT "PK_ocupaciones_plazas" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.ofrecimientos (
        "Id" uuid NOT NULL,
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
        CONSTRAINT "PK_ofrecimientos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.propuestas_ingresos (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "Motivo" text NOT NULL,
        "HuellaEntradas" character varying(64) NOT NULL,
        "Entradas" jsonb NOT NULL,
        "Resultados" jsonb NOT NULL,
        "PropuestaAnteriorId" uuid,
        "Estado" text NOT NULL,
        "Revision" integer NOT NULL,
        CONSTRAINT "PK_propuestas_ingresos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE TABLE vacantes.sorteos (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "Sala" integer NOT NULL,
        "Numero" integer NOT NULL,
        "Fecha" date NOT NULL,
        "Referencia" character varying(250) NOT NULL,
        "Version" integer NOT NULL,
        "Vigente" boolean NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "FechaRegistro" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_sorteos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_aceptaciones_turno_PropuestaId_SolicitudId_TurnoId" ON vacantes.aceptaciones_turno ("PropuestaId", "SolicitudId", "TurnoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE INDEX "IX_cambios_ofrecimientos_OfrecimientoId" ON vacantes.cambios_ofrecimientos ("OfrecimientoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE INDEX "IX_casos_revision_PeriodoId_Estado" ON vacantes.casos_revision ("PeriodoId", "Estado");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_casos_revision_PeriodoId_Sala_Tipo_HuellaOrigen" ON vacantes.casos_revision ("PeriodoId", "Sala", "Tipo", "HuellaOrigen");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE INDEX "IX_evaluaciones_prioridad_PeriodoId_Fecha" ON vacantes.evaluaciones_prioridad ("PeriodoId", "Fecha");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE INDEX "IX_historial_casos_revision_CasoRevisionId_Fecha" ON vacantes.historial_casos_revision ("CasoRevisionId", "Fecha");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_ingresos_confirmados_OfrecimientoId" ON vacantes.ingresos_confirmados ("OfrecimientoId") WHERE "OfrecimientoId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_ingresos_confirmados_PeriodoId_AspiranteId" ON vacantes.ingresos_confirmados ("PeriodoId", "AspiranteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_ocupaciones_plazas_PeriodoId_AspiranteId" ON vacantes.ocupaciones_plazas ("PeriodoId", "AspiranteId") WHERE "Activa" = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_ofrecimientos_PeriodoId_OperacionId" ON vacantes.ofrecimientos ("PeriodoId", "OperacionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE INDEX "IX_propuestas_ingresos_PeriodoId_Fecha" ON vacantes.propuestas_ingresos ("PeriodoId", "Fecha");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_sorteos_PeriodoId_Sala" ON vacantes.sorteos ("PeriodoId", "Sala") WHERE "Vigente" = true;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    CREATE UNIQUE INDEX "IX_sorteos_PeriodoId_Sala_Version" ON vacantes.sorteos ("PeriodoId", "Sala", "Version");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM vacantes."__EFMigrationsHistory" WHERE "MigrationId" = '20261105150312_InicialVacantes') THEN
    INSERT INTO vacantes."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261105150312_InicialVacantes', '10.0.13');
    END IF;
END $EF$;
