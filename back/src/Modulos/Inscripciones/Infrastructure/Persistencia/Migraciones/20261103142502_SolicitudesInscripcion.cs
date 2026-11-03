using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class SolicitudesInscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitudes",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AspiranteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomicilioDeclarado_Calle = table.Column<string>(type: "text", nullable: false),
                    DomicilioDeclarado_Numero = table.Column<string>(type: "text", nullable: false),
                    DomicilioDeclarado_Localidad = table.Column<string>(type: "text", nullable: false),
                    DomicilioDeclarado_Provincia = table.Column<string>(type: "text", nullable: false),
                    DomicilioDeclarado_CodigoPostal = table.Column<string>(type: "text", nullable: true),
                    DomicilioDeclarado_Complemento = table.Column<string>(type: "text", nullable: true),
                    DomicilioDeclarado_Piso = table.Column<string>(type: "text", nullable: true),
                    DomicilioDeclarado_Departamento = table.Column<string>(type: "text", nullable: true),
                    DomicilioDeclarado_Barrio = table.Column<string>(type: "text", nullable: true),
                    CalculoEdad_FechaNacimientoUtilizada = table.Column<DateOnly>(type: "date", nullable: false),
                    CalculoEdad_FechaCorteUtilizada = table.Column<DateOnly>(type: "date", nullable: false),
                    CalculoEdad_EdadCalculada = table.Column<int>(type: "integer", nullable: false),
                    CalculoEdad_Sala = table.Column<int>(type: "integer", nullable: true),
                    TurnoPreferidoId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionReglasId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionOfertaId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionPeriodoUtilizada = table.Column<int>(type: "integer", nullable: false),
                    EstadoRecepcion = table.Column<string>(type: "text", nullable: false),
                    UsuarioRegistroId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReferenciaConsentimiento = table.Column<string>(type: "text", nullable: true),
                    RevisionDatos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_solicitudes_aspirantes_instituciones_InstitucionId_Aspirant~",
                        columns: x => new { x.InstitucionId, x.AspiranteId },
                        principalSchema: "inscripciones",
                        principalTable: "aspirantes_instituciones",
                        principalColumns: new[] { "InstitucionId", "AspiranteId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "turnos_alternativos_solicitud",
                schema: "inscripciones",
                columns: table => new
                {
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turnos_alternativos_solicitud", x => new { x.SolicitudId, x.TurnoId });
                    table.ForeignKey(
                        name: "FK_turnos_alternativos_solicitud_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_AspiranteId_PeriodoId",
                schema: "inscripciones",
                table: "solicitudes",
                columns: new[] { "AspiranteId", "PeriodoId" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_InstitucionId_AspiranteId",
                schema: "inscripciones",
                table: "solicitudes",
                columns: new[] { "InstitucionId", "AspiranteId" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitudes_InstitucionId_PeriodoId",
                schema: "inscripciones",
                table: "solicitudes",
                columns: new[] { "InstitucionId", "PeriodoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "turnos_alternativos_solicitud",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "solicitudes",
                schema: "inscripciones");
        }
    }
}
