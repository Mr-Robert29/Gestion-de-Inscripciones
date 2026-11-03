using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Configuracion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AulasTurnosOferta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aulas",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aulas_instituciones_InstitucionId",
                        column: x => x.InstitucionId,
                        principalSchema: "configuracion",
                        principalTable: "instituciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "turnos",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_turnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_turnos_instituciones_InstitucionId",
                        column: x => x.InstitucionId,
                        principalSchema: "configuracion",
                        principalTable: "instituciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ofertas_aula_turno",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenciaCapacidad = table.Column<string>(type: "text", nullable: false),
                    OrigenOcupaciones = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofertas_aula_turno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ofertas_aula_turno_aulas_AulaId",
                        column: x => x.AulaId,
                        principalSchema: "configuracion",
                        principalTable: "aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ofertas_aula_turno_periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalSchema: "configuracion",
                        principalTable: "periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ofertas_aula_turno_turnos_TurnoId",
                        column: x => x.TurnoId,
                        principalSchema: "configuracion",
                        principalTable: "turnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ocupaciones_iniciales",
                schema: "configuracion",
                columns: table => new
                {
                    Sala = table.Column<int>(type: "integer", nullable: false),
                    OfertaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocupaciones_iniciales", x => new { x.OfertaId, x.Sala });
                    table.ForeignKey(
                        name: "FK_ocupaciones_iniciales_ofertas_aula_turno_OfertaId",
                        column: x => x.OfertaId,
                        principalSchema: "configuracion",
                        principalTable: "ofertas_aula_turno",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aulas_InstitucionId",
                schema: "configuracion",
                table: "aulas",
                column: "InstitucionId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_aula_turno_AulaId",
                schema: "configuracion",
                table: "ofertas_aula_turno",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_aula_turno_TurnoId",
                schema: "configuracion",
                table: "ofertas_aula_turno",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "UX_Oferta_Periodo_Aula_Turno",
                schema: "configuracion",
                table: "ofertas_aula_turno",
                columns: new[] { "PeriodoId", "AulaId", "TurnoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_turnos_InstitucionId",
                schema: "configuracion",
                table: "turnos",
                column: "InstitucionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ocupaciones_iniciales",
                schema: "configuracion");

            migrationBuilder.DropTable(
                name: "ofertas_aula_turno",
                schema: "configuracion");

            migrationBuilder.DropTable(
                name: "aulas",
                schema: "configuracion");

            migrationBuilder.DropTable(
                name: "turnos",
                schema: "configuracion");
        }
    }
}
