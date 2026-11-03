using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class RevisionManualDomicilios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "revisiones_domicilio",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroRegistro = table.Column<int>(type: "integer", nullable: false),
                    RevisionDomicilio = table.Column<int>(type: "integer", nullable: false),
                    RevisionSolicitudUtilizada = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Calle = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Numero = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Localidad = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Provincia = table.Column<string>(type: "text", nullable: false),
                    Domicilio_CodigoPostal = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Complemento = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Piso = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Departamento = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Barrio = table.Column<string>(type: "text", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revisiones_domicilio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_revisiones_domicilio_solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalSchema: "inscripciones",
                        principalTable: "solicitudes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_revisiones_domicilio_InstitucionId_SolicitudId",
                schema: "inscripciones",
                table: "revisiones_domicilio",
                columns: new[] { "InstitucionId", "SolicitudId" });

            migrationBuilder.CreateIndex(
                name: "UX_RevisionDomicilio_Solicitud_Numero",
                schema: "inscripciones",
                table: "revisiones_domicilio",
                columns: new[] { "SolicitudId", "NumeroRegistro" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "revisiones_domicilio",
                schema: "inscripciones");
        }
    }
}
