using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Configuracion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class OfertasReutilizablesPorInstitucion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ofertas_institucionales",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    ReferenciaCapacidad = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofertas_institucionales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ofertas_institucionales_aulas_AulaId",
                        column: x => x.AulaId,
                        principalSchema: "configuracion",
                        principalTable: "aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ofertas_institucionales_instituciones_InstitucionId",
                        column: x => x.InstitucionId,
                        principalSchema: "configuracion",
                        principalTable: "instituciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ofertas_institucionales_turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalSchema: "configuracion",
                        principalTable: "turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_institucionales_AulaId",
                schema: "configuracion",
                table: "ofertas_institucionales",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_institucionales_TurnoId",
                schema: "configuracion",
                table: "ofertas_institucionales",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "UX_OfertaInstitucional_Institucion_Aula_Turno",
                schema: "configuracion",
                table: "ofertas_institucionales",
                columns: new[] { "InstitucionId", "AulaId", "TurnoId" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO configuracion.ofertas_institucionales
                    ("Id", "InstitucionId", "AulaId", "TurnoId", "Capacidad", "ReferenciaCapacidad")
                SELECT DISTINCT ON (p."InstitucionId", o."AulaId", o."TurnoId")
                    o."Id", p."InstitucionId", o."AulaId", o."TurnoId", o."Capacidad", o."ReferenciaCapacidad"
                FROM configuracion.ofertas_aula_turno o
                INNER JOIN configuracion.periodos p ON p."Id" = o."PeriodoId"
                ORDER BY p."InstitucionId", o."AulaId", o."TurnoId", p."Ciclo" DESC, p."Fin" DESC, o."Id" DESC;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ofertas_institucionales",
                schema: "configuracion");
        }
    }
}
