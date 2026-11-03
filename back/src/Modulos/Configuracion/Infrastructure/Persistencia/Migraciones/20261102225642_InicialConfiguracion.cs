using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jardin.Configuracion.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class InicialConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "configuracion");

            migrationBuilder.CreateTable(
                name: "instituciones",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Calle = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Numero = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Localidad = table.Column<string>(type: "text", nullable: false),
                    Domicilio_Provincia = table.Column<string>(type: "text", nullable: false),
                    Domicilio_CodigoPostal = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Complemento = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Piso = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Departamento = table.Column<string>(type: "text", nullable: true),
                    Domicilio_Barrio = table.Column<string>(type: "text", nullable: true),
                    TelefonoContacto = table.Column<string>(type: "text", nullable: false),
                    CorreoElectronicoContacto = table.Column<string>(type: "text", nullable: true),
                    Coordenadas_Latitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Coordenadas_Longitud = table.Column<decimal>(type: "numeric", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    RevisionUbicacion = table.Column<int>(type: "integer", nullable: false),
                    RevisionDatos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instituciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "historial_instituciones",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Accion = table.Column<string>(type: "text", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: false),
                    DatosAnteriores = table.Column<string>(type: "text", nullable: true),
                    DatosNuevos = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_instituciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_historial_instituciones_instituciones_InstitucionId",
                        column: x => x.InstitucionId,
                        principalSchema: "configuracion",
                        principalTable: "instituciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usos_ubicacion",
                schema: "configuracion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitucionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    RevisionUbicacion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usos_ubicacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usos_ubicacion_instituciones_InstitucionId",
                        column: x => x.InstitucionId,
                        principalSchema: "configuracion",
                        principalTable: "instituciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_historial_instituciones_InstitucionId",
                schema: "configuracion",
                table: "historial_instituciones",
                column: "InstitucionId");

            migrationBuilder.CreateIndex(
                name: "IX_usos_ubicacion_InstitucionId_ReferenciaId_Tipo_RevisionUbic~",
                schema: "configuracion",
                table: "usos_ubicacion",
                columns: new[] { "InstitucionId", "ReferenciaId", "Tipo", "RevisionUbicacion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historial_instituciones",
                schema: "configuracion");

            migrationBuilder.DropTable(
                name: "usos_ubicacion",
                schema: "configuracion");

            migrationBuilder.DropTable(
                name: "instituciones",
                schema: "configuracion");
        }
    }
}
