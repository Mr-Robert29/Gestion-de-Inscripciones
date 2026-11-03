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
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'configuracion') THEN
        CREATE SCHEMA configuracion;
    END IF;
END $EF$;

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

CREATE TABLE configuracion.usos_ubicacion (
    "Id" uuid NOT NULL,
    "InstitucionId" uuid NOT NULL,
    "ReferenciaId" uuid NOT NULL,
    "Tipo" text NOT NULL,
    "RevisionUbicacion" integer NOT NULL,
    CONSTRAINT "PK_usos_ubicacion" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_usos_ubicacion_instituciones_InstitucionId" FOREIGN KEY ("InstitucionId") REFERENCES configuracion.instituciones ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_historial_instituciones_InstitucionId" ON configuracion.historial_instituciones ("InstitucionId");

CREATE UNIQUE INDEX "IX_usos_ubicacion_InstitucionId_ReferenciaId_Tipo_RevisionUbic~" ON configuracion.usos_ubicacion ("InstitucionId", "ReferenciaId", "Tipo", "RevisionUbicacion");

INSERT INTO configuracion."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261102225642_InicialConfiguracion', '10.0.13');

COMMIT;

