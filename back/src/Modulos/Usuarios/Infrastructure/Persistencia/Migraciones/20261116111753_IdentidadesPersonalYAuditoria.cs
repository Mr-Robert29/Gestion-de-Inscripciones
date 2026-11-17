using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Usuarios.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class IdentidadesPersonalYAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarClave",
                schema: "usuarios",
                table: "personal",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentidadPersonalId",
                schema: "usuarios",
                table: "personal",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "auditoria_personal",
                schema: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioAfectadoId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Accion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_personal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "identidades_personal",
                schema: "usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DniNormalizado = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identidades_personal", x => x.Id);
                });

            // Las cuentas existentes mantienen su independencia hasta que Root las vincule por DNI.
            migrationBuilder.Sql("INSERT INTO usuarios.identidades_personal (\"Id\") SELECT \"Id\" FROM usuarios.personal;");
            migrationBuilder.Sql("UPDATE usuarios.personal SET \"IdentidadPersonalId\" = \"Id\";");

            migrationBuilder.CreateIndex(
                name: "IX_personal_IdentidadPersonalId",
                schema: "usuarios",
                table: "personal",
                column: "IdentidadPersonalId");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_personal_FechaUtc",
                schema: "usuarios",
                table: "auditoria_personal",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_identidades_personal_DniNormalizado",
                schema: "usuarios",
                table: "identidades_personal",
                column: "DniNormalizado",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_personal_identidades_personal_IdentidadPersonalId",
                schema: "usuarios",
                table: "personal",
                column: "IdentidadPersonalId",
                principalSchema: "usuarios",
                principalTable: "identidades_personal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_personal_identidades_personal_IdentidadPersonalId",
                schema: "usuarios",
                table: "personal");

            migrationBuilder.DropTable(
                name: "auditoria_personal",
                schema: "usuarios");

            migrationBuilder.DropTable(
                name: "identidades_personal",
                schema: "usuarios");

            migrationBuilder.DropIndex(
                name: "IX_personal_IdentidadPersonalId",
                schema: "usuarios",
                table: "personal");

            migrationBuilder.DropColumn(
                name: "DebeCambiarClave",
                schema: "usuarios",
                table: "personal");

            migrationBuilder.DropColumn(
                name: "IdentidadPersonalId",
                schema: "usuarios",
                table: "personal");
        }
    }
}
