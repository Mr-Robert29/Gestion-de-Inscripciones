using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class DistanciasManuales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mediciones_distancia",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroRegistro = table.Column<int>(type: "integer", nullable: false),
                    RevisionDomicilio = table.Column<int>(type: "integer", nullable: false),
                    RevisionUbicacionJardin = table.Column<int>(type: "integer", nullable: false),
                    DistanciaMetros = table.Column<decimal>(type: "numeric", nullable: false),
                    Origen = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: false),
                    Entradas = table.Column<string>(type: "text", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mediciones_distancia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mediciones_distancia_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mediciones_distancia_InstitucionId_SolicitudId",
                schema: "inscripciones",
                table: "mediciones_distancia",
                columns: new[] { "InstitucionId", "SolicitudId" });

            migrationBuilder.CreateIndex(
                name: "UX_Distancia_Solicitud_Numero",
                schema: "inscripciones",
                table: "mediciones_distancia",
                columns: new[] { "SolicitudId", "NumeroRegistro" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mediciones_distancia",
                schema: "inscripciones");
        }
    }
}
