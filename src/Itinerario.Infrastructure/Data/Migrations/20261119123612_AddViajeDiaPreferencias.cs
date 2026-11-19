using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddViajeDiaPreferencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tipo_viaje",
                table: "dias_viaje",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "etiquetas_viaje_dia",
                columns: table => new
                {
                    viaje_dia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_codigo = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etiquetas_viaje_dia", x => new { x.viaje_dia_id, x.categoria_codigo });
                    table.ForeignKey(
                        name: "FK_etiquetas_viaje_dia_dias_viaje_viaje_dia_id",
                        column: x => x.viaje_dia_id,
                        principalTable: "dias_viaje",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etiquetas_viaje_dia");

            migrationBuilder.DropColumn(
                name: "tipo_viaje",
                table: "dias_viaje");
        }
    }
}
