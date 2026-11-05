using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Secciones.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class InicialSecciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "secciones");

            migrationBuilder.CreateTable(
                name: "asignaciones_seccion",
                schema: "secciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IngresoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asignaciones_seccion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cambios_baja_asignaciones",
                schema: "secciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsignacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngresoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaAnterior = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioAnteriorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MotivoAnterior = table.Column<string>(type: "text", nullable: true),
                    EstadoAnterior = table.Column<string>(type: "text", nullable: false),
                    UsuarioBajaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaBaja = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MotivoBaja = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cambios_baja_asignaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cambios_composicion",
                schema: "secciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeccionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdadesAnteriores = table.Column<string>(type: "text", nullable: false),
                    EdadesNuevas = table.Column<string>(type: "text", nullable: false),
                    RevisionAnterior = table.Column<int>(type: "integer", nullable: false),
                    RevisionNueva = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cambios_composicion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ingresos_retirados",
                schema: "secciones",
                columns: table => new
                {
                    IngresoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingresos_retirados", x => x.IngresoId);
                });

            migrationBuilder.CreateTable(
                name: "propuestas_distribucion",
                schema: "secciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    RevisionIngresos = table.Column<int>(type: "integer", nullable: false),
                    RevisionSecciones = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreadaPor = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmadaPor = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaConfirmacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_propuestas_distribucion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "secciones",
                schema: "secciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    edades_admitidas = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "asignaciones_propuesta",
                schema: "secciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropuestaDistribucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngresoId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeccionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Edad = table.Column<int>(type: "integer", nullable: false),
                    TurnoId = table.Column<Guid>(type: "uuid", nullable: false),
                    MotivoSinSeccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModificadaPor = table.Column<Guid>(type: "uuid", nullable: true),
                    ModificadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MotivoModificacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asignaciones_propuesta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asignaciones_propuesta_propuestas_distribucion_PropuestaDis~",
                        column: x => x.PropuestaDistribucionId,
                        principalSchema: "secciones",
                        principalTable: "propuestas_distribucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_propuesta_PropuestaDistribucionId_IngresoId",
                schema: "secciones",
                table: "asignaciones_propuesta",
                columns: new[] { "PropuestaDistribucionId", "IngresoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_seccion_IngresoId",
                schema: "secciones",
                table: "asignaciones_seccion",
                column: "IngresoId",
                unique: true,
                filter: "\"Estado\" = 'Activa'");

            migrationBuilder.CreateIndex(
                name: "IX_cambios_baja_asignaciones_IngresoId",
                schema: "secciones",
                table: "cambios_baja_asignaciones",
                column: "IngresoId");

            migrationBuilder.CreateIndex(
                name: "IX_cambios_composicion_SeccionId_RevisionNueva",
                schema: "secciones",
                table: "cambios_composicion",
                columns: new[] { "SeccionId", "RevisionNueva" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_propuestas_distribucion_PeriodoId_Version",
                schema: "secciones",
                table: "propuestas_distribucion",
                columns: new[] { "PeriodoId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_secciones_PeriodoId_AulaId_TurnoId",
                schema: "secciones",
                table: "secciones",
                columns: new[] { "PeriodoId", "AulaId", "TurnoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asignaciones_propuesta",
                schema: "secciones");

            migrationBuilder.DropTable(
                name: "asignaciones_seccion",
                schema: "secciones");

            migrationBuilder.DropTable(
                name: "cambios_baja_asignaciones",
                schema: "secciones");

            migrationBuilder.DropTable(
                name: "cambios_composicion",
                schema: "secciones");

            migrationBuilder.DropTable(
                name: "ingresos_retirados",
                schema: "secciones");

            migrationBuilder.DropTable(
                name: "secciones",
                schema: "secciones");

            migrationBuilder.DropTable(
                name: "propuestas_distribucion",
                schema: "secciones");
        }
    }
}
