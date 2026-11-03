DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'inscripciones') THEN
        CREATE SCHEMA inscripciones;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS inscripciones."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'inscripciones') THEN
            CREATE SCHEMA inscripciones;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE TABLE inscripciones.aspirantes (
        "Id" uuid NOT NULL,
        "Nombre" text NOT NULL,
        "Apellido" text NOT NULL,
        "Dni" text,
        "FechaNacimiento" date NOT NULL,
        "TipoDocumento" text,
        "Sexo" text,
        CONSTRAINT "PK_aspirantes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE TABLE inscripciones.historial_personas (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "EntidadId" uuid NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "Accion" text NOT NULL,
        "Datos" text NOT NULL,
        CONSTRAINT "PK_historial_personas" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE TABLE inscripciones.responsables (
        "Id" uuid NOT NULL,
        "Nombre" text NOT NULL,
        "Apellido" text NOT NULL,
        "Documento" text,
        "TipoDocumento" text,
        "Telefono" text NOT NULL,
        "CorreoElectronico" text,
        CONSTRAINT "PK_responsables" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE TABLE inscripciones.aspirantes_instituciones (
        "InstitucionId" uuid NOT NULL,
        "AspiranteId" uuid NOT NULL,
        CONSTRAINT "PK_aspirantes_instituciones" PRIMARY KEY ("InstitucionId", "AspiranteId"),
        CONSTRAINT "FK_aspirantes_instituciones_aspirantes_AspiranteId" FOREIGN KEY ("AspiranteId") REFERENCES inscripciones.aspirantes ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE TABLE inscripciones.responsables_instituciones (
        "InstitucionId" uuid NOT NULL,
        "ResponsableId" uuid NOT NULL,
        CONSTRAINT "PK_responsables_instituciones" PRIMARY KEY ("InstitucionId", "ResponsableId"),
        CONSTRAINT "FK_responsables_instituciones_responsables_ResponsableId" FOREIGN KEY ("ResponsableId") REFERENCES inscripciones.responsables ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE TABLE inscripciones.vinculos_responsables (
        "AspiranteId" uuid NOT NULL,
        "ResponsableId" uuid NOT NULL,
        "TipoVinculo" text NOT NULL,
        "ContactoPrincipal" boolean NOT NULL,
        "EsTutor" boolean NOT NULL,
        CONSTRAINT "PK_vinculos_responsables" PRIMARY KEY ("AspiranteId", "ResponsableId"),
        CONSTRAINT "FK_vinculos_responsables_aspirantes_AspiranteId" FOREIGN KEY ("AspiranteId") REFERENCES inscripciones.aspirantes ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_vinculos_responsables_responsables_ResponsableId" FOREIGN KEY ("ResponsableId") REFERENCES inscripciones.responsables ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE INDEX "IX_aspirantes_Dni" ON inscripciones.aspirantes ("Dni");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE INDEX "IX_aspirantes_instituciones_AspiranteId" ON inscripciones.aspirantes_instituciones ("AspiranteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE INDEX "IX_historial_personas_InstitucionId" ON inscripciones.historial_personas ("InstitucionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE INDEX "IX_responsables_Documento" ON inscripciones.responsables ("Documento");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE INDEX "IX_responsables_instituciones_ResponsableId" ON inscripciones.responsables_instituciones ("ResponsableId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    CREATE INDEX "IX_vinculos_responsables_ResponsableId" ON inscripciones.vinculos_responsables ("ResponsableId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103135249_PersonasYResponsables') THEN
    INSERT INTO inscripciones."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103135249_PersonasYResponsables', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103142502_SolicitudesInscripcion') THEN
    CREATE TABLE inscripciones.solicitudes (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "AspiranteId" uuid NOT NULL,
        "DomicilioDeclarado_Calle" text NOT NULL,
        "DomicilioDeclarado_Numero" text NOT NULL,
        "DomicilioDeclarado_Localidad" text NOT NULL,
        "DomicilioDeclarado_Provincia" text NOT NULL,
        "DomicilioDeclarado_CodigoPostal" text,
        "DomicilioDeclarado_Complemento" text,
        "DomicilioDeclarado_Piso" text,
        "DomicilioDeclarado_Departamento" text,
        "DomicilioDeclarado_Barrio" text,
        "CalculoEdad_FechaNacimientoUtilizada" date NOT NULL,
        "CalculoEdad_FechaCorteUtilizada" date NOT NULL,
        "CalculoEdad_EdadCalculada" integer NOT NULL,
        "CalculoEdad_Sala" integer,
        "TurnoPreferidoId" uuid NOT NULL,
        "VersionReglasId" uuid NOT NULL,
        "VersionOfertaId" uuid NOT NULL,
        "RevisionPeriodoUtilizada" integer NOT NULL,
        "EstadoRecepcion" text NOT NULL,
        "UsuarioRegistroId" uuid NOT NULL,
        "FechaRegistro" timestamp with time zone NOT NULL,
        "ReferenciaConsentimiento" text,
        "RevisionDatos" integer NOT NULL,
        CONSTRAINT "PK_solicitudes" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_solicitudes_aspirantes_instituciones_InstitucionId_Aspirant~" FOREIGN KEY ("InstitucionId", "AspiranteId") REFERENCES inscripciones.aspirantes_instituciones ("InstitucionId", "AspiranteId") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103142502_SolicitudesInscripcion') THEN
    CREATE TABLE inscripciones.turnos_alternativos_solicitud (
        "TurnoId" uuid NOT NULL,
        "SolicitudId" uuid NOT NULL,
        CONSTRAINT "PK_turnos_alternativos_solicitud" PRIMARY KEY ("SolicitudId", "TurnoId"),
        CONSTRAINT "FK_turnos_alternativos_solicitud_solicitudes_SolicitudId" FOREIGN KEY ("SolicitudId") REFERENCES inscripciones.solicitudes ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103142502_SolicitudesInscripcion') THEN
    CREATE INDEX "IX_solicitudes_AspiranteId_PeriodoId" ON inscripciones.solicitudes ("AspiranteId", "PeriodoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103142502_SolicitudesInscripcion') THEN
    CREATE INDEX "IX_solicitudes_InstitucionId_AspiranteId" ON inscripciones.solicitudes ("InstitucionId", "AspiranteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103142502_SolicitudesInscripcion') THEN
    CREATE INDEX "IX_solicitudes_InstitucionId_PeriodoId" ON inscripciones.solicitudes ("InstitucionId", "PeriodoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM inscripciones."__EFMigrationsHistory" WHERE "MigrationId" = '20261103142502_SolicitudesInscripcion') THEN
    INSERT INTO inscripciones."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103142502_SolicitudesInscripcion', '10.0.13');
    END IF;
END $EF$;
COMMIT;

