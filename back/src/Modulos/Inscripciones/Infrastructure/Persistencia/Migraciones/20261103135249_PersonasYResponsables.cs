using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Inscripciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class PersonasYResponsables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inscripciones");

            migrationBuilder.CreateTable(
                name: "aspirantes",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Apellido = table.Column<string>(type: "text", nullable: false),
                    Dni = table.Column<string>(type: "text", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoDocumento = table.Column<string>(type: "text", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aspirantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "historial_personas",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: false),
                    Datos = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_personas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "responsables",
                schema: "inscripciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Apellido = table.Column<string>(type: "text", nullable: false),
                    Documento = table.Column<string>(type: "text", nullable: true),
                    TipoDocumento = table.Column<string>(type: "text", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: false),
                    CorreoElectronico = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responsables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aspirantes_instituciones",
                schema: "inscripciones",
                columns: table => new
                {
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AspiranteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aspirantes_instituciones", x => new { x.InstitucionId, x.AspiranteId });
                    table.ForeignKey(
                        name: "FK_aspirantes_instituciones_aspirantes_AspiranteId",
                        column: x => x.AspiranteId,
                        principalSchema: "inscripciones",
                        principalTable: "aspirantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "responsables_instituciones",
                schema: "inscripciones",
                columns: table => new
                {
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsableId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_responsables_instituciones", x => new { x.InstitucionId, x.ResponsableId });
                    table.ForeignKey(
                        name: "FK_responsables_instituciones_responsables_ResponsableId",
                        column: x => x.ResponsableId,
                        principalSchema: "inscripciones",
                        principalTable: "responsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vinculos_responsables",
                schema: "inscripciones",
                columns: table => new
                {
                    AspiranteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsableId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoVinculo = table.Column<string>(type: "text", nullable: false),
                    ContactoPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    EsTutor = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vinculos_responsables", x => new { x.AspiranteId, x.ResponsableId });
                    table.ForeignKey(
                        name: "FK_vinculos_responsables_aspirantes_AspiranteId",
                        column: x => x.AspiranteId,
                        principalSchema: "inscripciones",
                        principalTable: "aspirantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vinculos_responsables_responsables_ResponsableId",
                        column: x => x.ResponsableId,
                        principalSchema: "inscripciones",
                        principalTable: "responsables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aspirantes_Dni",
                schema: "inscripciones",
                table: "aspirantes",
                column: "Dni");

            migrationBuilder.CreateIndex(
                name: "IX_aspirantes_instituciones_AspiranteId",
                schema: "inscripciones",
                table: "aspirantes_instituciones",
                column: "AspiranteId");

            migrationBuilder.CreateIndex(
                name: "IX_historial_personas_InstitucionId",
                schema: "inscripciones",
                table: "historial_personas",
                column: "InstitucionId");

            migrationBuilder.CreateIndex(
                name: "IX_responsables_Documento",
                schema: "inscripciones",
                table: "responsables",
                column: "Documento");

            migrationBuilder.CreateIndex(
                name: "IX_responsables_instituciones_ResponsableId",
                schema: "inscripciones",
                table: "responsables_instituciones",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_vinculos_responsables_ResponsableId",
                schema: "inscripciones",
                table: "vinculos_responsables",
                column: "ResponsableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aspirantes_instituciones",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "historial_personas",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "responsables_instituciones",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "vinculos_responsables",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "aspirantes",
                schema: "inscripciones");

            migrationBuilder.DropTable(
                name: "responsables",
                schema: "inscripciones");
        }
    }
}
