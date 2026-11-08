using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EtiquetasPorCodigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_etiquetas_viaje",
                table: "etiquetas_viaje");

            migrationBuilder.DropColumn(
                name: "categoria_id",
                table: "etiquetas_viaje");

            migrationBuilder.AddColumn<string>(
                name: "categoria_codigo",
                table: "etiquetas_viaje",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_etiquetas_viaje",
                table: "etiquetas_viaje",
                columns: new[] { "viaje_id", "categoria_codigo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_etiquetas_viaje",
                table: "etiquetas_viaje");

            migrationBuilder.DropColumn(
                name: "categoria_codigo",
                table: "etiquetas_viaje");

            migrationBuilder.AddColumn<short>(
                name: "categoria_id",
                table: "etiquetas_viaje",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_etiquetas_viaje",
                table: "etiquetas_viaje",
                columns: new[] { "viaje_id", "categoria_id" });
        }
    }
}
