using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class DocumentacionYCondiciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "condiciones_prioridad",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    ValorDeclarado = table.Column<string>(type: "text", nullable: false),
                    EstadoVerificacion = table.Column<string>(type: "text", nullable: false),
                    EvidenciaReferencia = table.Column<string>(type: "text", nullable: true),
                    Observacion = table.Column<string>(type: "text", nullable: true),
                    UsuarioRegistroId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaRevision = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_condiciones_prioridad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_condiciones_prioridad_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentacion_solicitud",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: true),
                    Observacion = table.Column<string>(type: "text", nullable: true),
                    UsuarioRegistroId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaRegistro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaRevision = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentacion_solicitud", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documentacion_solicitud_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_condiciones_prioridad_SolicitudId",
                schema: "inscripciones",
                table: "condiciones_prioridad",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_documentacion_solicitud_SolicitudId",
                schema: "inscripciones",
                table: "documentacion_solicitud",
                column: "SolicitudId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "condiciones_prioridad",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "documentacion_solicitud",
                schema: "inscripciones");
        }
    }
}
