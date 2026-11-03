using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CorreccionesEImpactoSolicitudes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RevisionDomicilio",
                schema: "inscripciones",
                table: "solicitudes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "usos_datos_solicitud",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    RevisionDatos = table.Column<int>(type: "integer", nullable: false),
                    RevisionDomicilio = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usos_datos_solicitud", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usos_datos_solicitud_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_UsoSolicitud_Referencia_Revision",
                schema: "inscripciones",
                table: "usos_datos_solicitud",
                columns: new[] { "SolicitudId", "ReferenciaId", "Tipo", "RevisionDatos", "RevisionDomicilio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usos_datos_solicitud",
                schema: "inscripciones");

            migrationBuilder.DropColumn(
                name: "RevisionDomicilio",
                schema: "inscripciones",
                table: "solicitudes");
        }
    }
}
