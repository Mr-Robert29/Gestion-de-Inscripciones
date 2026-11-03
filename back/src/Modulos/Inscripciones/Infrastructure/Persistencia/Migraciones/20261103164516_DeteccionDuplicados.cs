using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class DeteccionDuplicados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "posibles_duplicados",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudBId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilDeteccion = table.Column<string>(type: "text", nullable: false),
                    Criterios = table.Column<string>(type: "text", nullable: false),
                    HuellaDatos = table.Column<string>(type: "text", nullable: false),
                    DatosEvaluados = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    UsuarioDeteccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaDeteccion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioUltimaDeteccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaUltimaDeteccion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioResolucionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaResolucion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoResolucion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posibles_duplicados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_posibles_duplicados_solicitudes_SolicitudAId",
                        column: x => x.SolicitudAId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_posibles_duplicados_solicitudes_SolicitudBId",
                        column: x => x.SolicitudBId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_posibles_duplicados_InstitucionId_PeriodoId_Estado",
                schema: "inscripciones",
                table: "posibles_duplicados",
                columns: new[] { "InstitucionId", "PeriodoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_posibles_duplicados_SolicitudBId",
                schema: "inscripciones",
                table: "posibles_duplicados",
                column: "SolicitudBId");

            migrationBuilder.CreateIndex(
                name: "UX_Duplicado_ParSolicitudes",
                schema: "inscripciones",
                table: "posibles_duplicados",
                columns: new[] { "SolicitudAId", "SolicitudBId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "posibles_duplicados",
                schema: "inscripciones");
        }
    }
}
