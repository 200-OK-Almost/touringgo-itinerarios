using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class QuitarDefaultMonedaGastosYLiquidaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos");

            migrationBuilder.AlterColumn<string>(
                name: "Moneda",
                table: "gastos",
                type: "char(3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "char(3)",
                oldDefaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos",
                sql: """ "Monto" > 0 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos");

            migrationBuilder.AlterColumn<int>(
                name: "Moneda",
                table: "gastos",
                type: "char(3)",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "char(3)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos",
                sql: """ "Monto" > 0 """);
        }
    }
}
