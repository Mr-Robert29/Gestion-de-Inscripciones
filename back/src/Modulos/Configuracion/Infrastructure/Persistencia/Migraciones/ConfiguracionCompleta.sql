DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'configuracion') THEN
        CREATE SCHEMA configuracion;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS configuracion."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'configuracion') THEN
            CREATE SCHEMA configuracion;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
    CREATE TABLE configuracion.instituciones (
        "Id" uuid NOT NULL,
        "Nombre" text NOT NULL,
        "Codigo" text,
        "Domicilio_Calle" text NOT NULL,
        "Domicilio_Numero" text NOT NULL,
        "Domicilio_Localidad" text NOT NULL,
        "Domicilio_Provincia" text NOT NULL,
        "Domicilio_CodigoPostal" text,
        "Domicilio_Complemento" text,
        "Domicilio_Piso" text,
        "Domicilio_Departamento" text,
        "Domicilio_Barrio" text,
        "TelefonoContacto" text NOT NULL,
        "CorreoElectronicoContacto" text,
        "Coordenadas_Latitud" numeric,
        "Coordenadas_Longitud" numeric,
        "Activa" boolean NOT NULL,
        "RevisionUbicacion" integer NOT NULL,
        "RevisionDatos" integer NOT NULL,
        CONSTRAINT "PK_instituciones" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
    CREATE TABLE configuracion.historial_instituciones (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "UsuarioId" uuid NOT NULL,
        "Fecha" timestamp with time zone NOT NULL,
        "Accion" text NOT NULL,
        "Motivo" text NOT NULL,
        "DatosAnteriores" text,
        "DatosNuevos" text NOT NULL,
        CONSTRAINT "PK_historial_instituciones" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_historial_instituciones_instituciones_InstitucionId" FOREIGN KEY ("InstitucionId") REFERENCES configuracion.instituciones ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
    CREATE TABLE configuracion.usos_ubicacion (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "ReferenciaId" uuid NOT NULL,
        "Tipo" text NOT NULL,
        "RevisionUbicacion" integer NOT NULL,
        CONSTRAINT "PK_usos_ubicacion" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_usos_ubicacion_instituciones_InstitucionId" FOREIGN KEY ("InstitucionId") REFERENCES configuracion.instituciones ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
    CREATE INDEX "IX_historial_instituciones_InstitucionId" ON configuracion.historial_instituciones ("InstitucionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
    CREATE UNIQUE INDEX "IX_usos_ubicacion_InstitucionId_ReferenciaId_Tipo_RevisionUbic~" ON configuracion.usos_ubicacion ("InstitucionId", "ReferenciaId", "Tipo", "RevisionUbicacion");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102225642_InicialConfiguracion') THEN
    INSERT INTO configuracion."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261102225642_InicialConfiguracion', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102234604_PeriodosInscripcion') THEN
    CREATE TABLE configuracion.periodos (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "Ciclo" integer NOT NULL,
        "Inicio" date NOT NULL,
        "Fin" date NOT NULL,
        "Estado" text NOT NULL,
        "Revision" integer NOT NULL,
        CONSTRAINT "PK_periodos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_periodos_instituciones_InstitucionId" FOREIGN KEY ("InstitucionId") REFERENCES configuracion.instituciones ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102234604_PeriodosInscripcion') THEN
    CREATE INDEX "IX_periodos_InstitucionId" ON configuracion.periodos ("InstitucionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261102234604_PeriodosInscripcion') THEN
    INSERT INTO configuracion."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261102234604_PeriodosInscripcion', '10.0.13');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE TABLE configuracion.aulas (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "Nombre" text NOT NULL,
        CONSTRAINT "PK_aulas" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_aulas_instituciones_InstitucionId" FOREIGN KEY ("InstitucionId") REFERENCES configuracion.instituciones ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE TABLE configuracion.turnos (
        "Id" uuid NOT NULL,
        "InstitucionId" uuid NOT NULL,
        "Nombre" text NOT NULL,
        CONSTRAINT "PK_turnos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_turnos_instituciones_InstitucionId" FOREIGN KEY ("InstitucionId") REFERENCES configuracion.instituciones ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE TABLE configuracion.ofertas_aula_turno (
        "Id" uuid NOT NULL,
        "PeriodoId" uuid NOT NULL,
        "AulaId" uuid NOT NULL,
        "TurnoId" uuid NOT NULL,
        "Capacidad" integer NOT NULL,
        "Disponible" boolean NOT NULL,
        "ReferenciaCapacidad" text NOT NULL,
        "OrigenOcupaciones" text NOT NULL,
        "Revision" integer NOT NULL,
        CONSTRAINT "PK_ofertas_aula_turno" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ofertas_aula_turno_aulas_AulaId" FOREIGN KEY ("AulaId") REFERENCES configuracion.aulas ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ofertas_aula_turno_periodos_PeriodoId" FOREIGN KEY ("PeriodoId") REFERENCES configuracion.periodos ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_ofertas_aula_turno_turnos_TurnoId" FOREIGN KEY ("TurnoId") REFERENCES configuracion.turnos ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE TABLE configuracion.ocupaciones_iniciales (
        "Sala" integer NOT NULL,
        "OfertaId" uuid NOT NULL,
        "Cantidad" integer NOT NULL,
        CONSTRAINT "PK_ocupaciones_iniciales" PRIMARY KEY ("OfertaId", "Sala"),
        CONSTRAINT "FK_ocupaciones_iniciales_ofertas_aula_turno_OfertaId" FOREIGN KEY ("OfertaId") REFERENCES configuracion.ofertas_aula_turno ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE INDEX "IX_aulas_InstitucionId" ON configuracion.aulas ("InstitucionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE INDEX "IX_ofertas_aula_turno_AulaId" ON configuracion.ofertas_aula_turno ("AulaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE INDEX "IX_ofertas_aula_turno_TurnoId" ON configuracion.ofertas_aula_turno ("TurnoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE UNIQUE INDEX "UX_Oferta_Periodo_Aula_Turno" ON configuracion.ofertas_aula_turno ("PeriodoId", "AulaId", "TurnoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    CREATE INDEX "IX_turnos_InstitucionId" ON configuracion.turnos ("InstitucionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM configuracion."__EFMigrationsHistory" WHERE "MigrationId" = '20261103013609_AulasTurnosOferta') THEN
    INSERT INTO configuracion."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261103013609_AulasTurnosOferta', '10.0.13');
    END IF;
END $EF$;
COMMIT;

