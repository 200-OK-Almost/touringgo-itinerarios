using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddViajeDias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dias_viaje",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_dia = table.Column<short>(type: "smallint", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    barrio = table.Column<string>(type: "text", nullable: false),
                    tipo_punto_partida = table.Column<string>(type: "text", nullable: false),
                    lugar_partida_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre_punto_partida = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    direccion_partida = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    latitud_partida = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    longitud_partida = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dias_viaje", x => x.id);
                    table.ForeignKey(
                        name: "FK_dias_viaje_viajes_viaje_id",
                        column: x => x.viaje_id,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dias_viaje_viaje_id_numero_dia",
                table: "dias_viaje",
                columns: new[] { "viaje_id", "numero_dia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dias_viaje");
        }
    }
}
