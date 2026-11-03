using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Configuracion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class VersionesConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VersionOfertaId",
                schema: "configuracion",
                table: "periodos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VersionReglasId",
                schema: "configuracion",
                table: "periodos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "versiones_configuracion",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Formato = table.Column<int>(type: "integer", nullable: false),
                    Datos = table.Column<string>(type: "text", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_versiones_configuracion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_versiones_configuracion_periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalSchema: "configuracion",
                        principalTable: "periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_periodos_VersionOfertaId",
                schema: "configuracion",
                table: "periodos",
                column: "VersionOfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_periodos_VersionReglasId",
                schema: "configuracion",
                table: "periodos",
                column: "VersionReglasId");

            migrationBuilder.CreateIndex(
                name: "IX_versiones_configuracion_PeriodoId",
                schema: "configuracion",
                table: "versiones_configuracion",
                column: "PeriodoId");

            migrationBuilder.AddForeignKey(
                name: "FK_periodos_versiones_configuracion_VersionOfertaId",
                schema: "configuracion",
                table: "periodos",
                column: "VersionOfertaId",
                principalSchema: "configuracion",
                principalTable: "versiones_configuracion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_periodos_versiones_configuracion_VersionReglasId",
                schema: "configuracion",
                table: "periodos",
                column: "VersionReglasId",
                principalSchema: "configuracion",
                principalTable: "versiones_configuracion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_periodos_versiones_configuracion_VersionOfertaId",
                schema: "configuracion",
                table: "periodos");

            migrationBuilder.DropForeignKey(
                name: "FK_periodos_versiones_configuracion_VersionReglasId",
                schema: "configuracion",
                table: "periodos");

            migrationBuilder.DropTable(
                name: "versiones_configuracion",
                schema: "configuracion");

            migrationBuilder.DropIndex(
                name: "IX_periodos_VersionOfertaId",
                schema: "configuracion",
                table: "periodos");

            migrationBuilder.DropIndex(
                name: "IX_periodos_VersionReglasId",
                schema: "configuracion",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "VersionOfertaId",
                schema: "configuracion",
                table: "periodos");

            migrationBuilder.DropColumn(
                name: "VersionReglasId",
                schema: "configuracion",
                table: "periodos");
        }
    }
}
