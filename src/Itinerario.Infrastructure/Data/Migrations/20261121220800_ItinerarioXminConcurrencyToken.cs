using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ItinerarioXminConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "itinerarios",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "itinerarios");
        }
    }
}
