using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Vacantes.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class InicialVacantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vacantes");

            migrationBuilder.CreateTable(
                name: "aceptaciones_turno",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropuestaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aceptaciones_turno", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cambios_ofrecimientos",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfrecimientoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "text", nullable: false),
                    EstadoNuevo = table.Column<string>(type: "text", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cambios_ofrecimientos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "casos_revision",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sala = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    SolicitudesAfectadas = table.Column<string>(type: "jsonb", nullable: false),
                    OrdenDecidido = table.Column<string>(type: "jsonb", nullable: false),
                    HuellaOrigen = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SorteoId = table.Column<Guid>(type: "uuid", nullable: true),
                    VersionSorteo = table.Column<int>(type: "integer", nullable: true),
                    VersionReglasId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioResolucion = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaResolucion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoResolucion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_casos_revision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evaluaciones_prioridad",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionReglasId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionPeriodo = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Datos = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluaciones_prioridad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "historial_casos_revision",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CasoRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EstadoAnterior = table.Column<string>(type: "text", nullable: false),
                    EstadoNuevo = table.Column<string>(type: "text", nullable: false),
                    OrdenAnterior = table.Column<string>(type: "jsonb", nullable: false),
                    OrdenNueva = table.Column<string>(type: "jsonb", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_casos_revision", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ingresos_confirmados",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    AspiranteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Edad = table.Column<int>(type: "integer", nullable: false),
                    OfertaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sala = table.Column<int>(type: "integer", nullable: false),
                    PropuestaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    BajaEnProceso = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioBajaId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaSolicitudBaja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FechaBaja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoBaja = table.Column<string>(type: "text", nullable: true),
                    OfrecimientoId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingresos_confirmados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ocupaciones_plazas",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AspiranteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfertaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sala = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocupaciones_plazas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ofrecimientos",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    AspiranteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfertaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropuestaListaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    UsuarioRespuestaId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaRespuesta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoRespuesta = table.Column<string>(type: "text", nullable: true),
                    ReferenciaRespuesta = table.Column<string>(type: "text", nullable: true),
                    IngresoId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofrecimientos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "propuestas_ingresos",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    HuellaEntradas = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Entradas = table.Column<string>(type: "jsonb", nullable: false),
                    Resultados = table.Column<string>(type: "jsonb", nullable: false),
                    PropuestaAnteriorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_propuestas_ingresos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sorteos",
                schema: "vacantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sala = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Vigente = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sorteos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aceptaciones_turno_PropuestaId_SolicitudId_TurnoId",
                schema: "vacantes",
                table: "aceptaciones_turno",
                columns: new[] { "PropuestaId", "SolicitudId", "TurnoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cambios_ofrecimientos_OfrecimientoId",
                schema: "vacantes",
                table: "cambios_ofrecimientos",
                column: "OfrecimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_casos_revision_PeriodoId_Estado",
                schema: "vacantes",
                table: "casos_revision",
                columns: new[] { "PeriodoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_casos_revision_PeriodoId_Sala_Tipo_HuellaOrigen",
                schema: "vacantes",
                table: "casos_revision",
                columns: new[] { "PeriodoId", "Sala", "Tipo", "HuellaOrigen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluaciones_prioridad_PeriodoId_Fecha",
                schema: "vacantes",
                table: "evaluaciones_prioridad",
                columns: new[] { "PeriodoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_historial_casos_revision_CasoRevisionId_Fecha",
                schema: "vacantes",
                table: "historial_casos_revision",
                columns: new[] { "CasoRevisionId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ingresos_confirmados_OfrecimientoId",
                schema: "vacantes",
                table: "ingresos_confirmados",
                column: "OfrecimientoId",
                unique: true,
                filter: "\"OfrecimientoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ingresos_confirmados_PeriodoId_AspiranteId",
                schema: "vacantes",
                table: "ingresos_confirmados",
                columns: new[] { "PeriodoId", "AspiranteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocupaciones_plazas_PeriodoId_AspiranteId",
                schema: "vacantes",
                table: "ocupaciones_plazas",
                columns: new[] { "PeriodoId", "AspiranteId" },
                unique: true,
                filter: "\"Activa\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ofrecimientos_PeriodoId_OperacionId",
                schema: "vacantes",
                table: "ofrecimientos",
                columns: new[] { "PeriodoId", "OperacionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_propuestas_ingresos_PeriodoId_Fecha",
                schema: "vacantes",
                table: "propuestas_ingresos",
                columns: new[] { "PeriodoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_sorteos_PeriodoId_Sala",
                schema: "vacantes",
                table: "sorteos",
                columns: new[] { "PeriodoId", "Sala" },
                unique: true,
                filter: "\"Vigente\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_sorteos_PeriodoId_Sala_Version",
                schema: "vacantes",
                table: "sorteos",
                columns: new[] { "PeriodoId", "Sala", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aceptaciones_turno",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "cambios_ofrecimientos",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "casos_revision",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "evaluaciones_prioridad",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "historial_casos_revision",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "ingresos_confirmados",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "ocupaciones_plazas",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "ofrecimientos",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "propuestas_ingresos",
                schema: "vacantes");

            migrationBuilder.DropTable(
                name: "sorteos",
                schema: "vacantes");
        }
    }
}
