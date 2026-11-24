using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelacionesGastos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_division_gastos_gastos_GastoId",
                table: "division_gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_gastos_liquidaciones_LiquidacionId",
                table: "gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_transferencias_liquidacion_liquidaciones_LiquidacionId",
                table: "transferencias_liquidacion");

            migrationBuilder.DropForeignKey(
                name: "FK_transferencias_liquidacion_miembros_viaje_DeParticipanteVia~",
                table: "transferencias_liquidacion");

            migrationBuilder.DropIndex(
                name: "IX_transferencias_liquidacion_DeParticipanteViajeId_DeParticip~",
                table: "transferencias_liquidacion");

            migrationBuilder.DropIndex(
                name: "IX_gastos_LiquidacionId",
                table: "gastos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos");

            migrationBuilder.DropColumn(
                name: "DeParticipanteUsuarioId",
                table: "transferencias_liquidacion");

            migrationBuilder.DropColumn(
                name: "DeParticipanteViajeId",
                table: "transferencias_liquidacion");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_liquidaciones_Id_ViajeId",
                table: "liquidaciones",
                columns: new[] { "Id", "ViajeId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_gastos_Id_ViajeId",
                table: "gastos",
                columns: new[] { "Id", "ViajeId" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_liquidacion_LiquidacionId_ViajeId",
                table: "transferencias_liquidacion",
                columns: new[] { "LiquidacionId", "ViajeId" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_liquidacion_ViajeId_AParticipanteId",
                table: "transferencias_liquidacion",
                columns: new[] { "ViajeId", "AParticipanteId" });

            migrationBuilder.CreateIndex(
                name: "IX_gastos_LiquidacionId_ViajeId",
                table: "gastos",
                columns: new[] { "LiquidacionId", "ViajeId" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos",
                sql: " \"Monto\" > 0 ");

            migrationBuilder.CreateIndex(
                name: "IX_division_gastos_GastoId_ViajeId",
                table: "division_gastos",
                columns: new[] { "GastoId", "ViajeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_division_gastos_gastos_GastoId_ViajeId",
                table: "division_gastos",
                columns: new[] { "GastoId", "ViajeId" },
                principalTable: "gastos",
                principalColumns: new[] { "Id", "ViajeId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gastos_liquidaciones_LiquidacionId_ViajeId",
                table: "gastos",
                columns: new[] { "LiquidacionId", "ViajeId" },
                principalTable: "liquidaciones",
                principalColumns: new[] { "Id", "ViajeId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transferencias_liquidacion_liquidaciones_LiquidacionId_Viaj~",
                table: "transferencias_liquidacion",
                columns: new[] { "LiquidacionId", "ViajeId" },
                principalTable: "liquidaciones",
                principalColumns: new[] { "Id", "ViajeId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_transferencias_liquidacion_miembros_viaje_ViajeId_AParticip~",
                table: "transferencias_liquidacion",
                columns: new[] { "ViajeId", "AParticipanteId" },
                principalTable: "miembros_viaje",
                principalColumns: new[] { "viaje_id", "usuario_id" },
                onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_division_gastos_gastos_GastoId_ViajeId",
                table: "division_gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_gastos_liquidaciones_LiquidacionId_ViajeId",
                table: "gastos");

            migrationBuilder.DropForeignKey(
                name: "FK_transferencias_liquidacion_liquidaciones_LiquidacionId_Viaj~",
                table: "transferencias_liquidacion");

            migrationBuilder.DropForeignKey(
                name: "FK_transferencias_liquidacion_miembros_viaje_ViajeId_AParticip~",
                table: "transferencias_liquidacion");

            migrationBuilder.DropIndex(
                name: "IX_transferencias_liquidacion_LiquidacionId_ViajeId",
                table: "transferencias_liquidacion");

            migrationBuilder.DropIndex(
                name: "IX_transferencias_liquidacion_ViajeId_AParticipanteId",
                table: "transferencias_liquidacion");
    
            migrationBuilder.DropUniqueConstraint(
                name: "AK_liquidaciones_Id_ViajeId",
                table: "liquidaciones");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_gastos_Id_ViajeId",
                table: "gastos");

            migrationBuilder.DropIndex(
                name: "IX_gastos_LiquidacionId_ViajeId",
                table: "gastos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos");

            migrationBuilder.DropIndex(
                name: "IX_division_gastos_GastoId_ViajeId",
                table: "division_gastos");

            migrationBuilder.AddColumn<Guid>(
                name: "DeParticipanteUsuarioId",
                table: "transferencias_liquidacion",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DeParticipanteViajeId",
                table: "transferencias_liquidacion",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_liquidacion_DeParticipanteViajeId_DeParticip~",
                table: "transferencias_liquidacion",
                columns: new[] { "DeParticipanteViajeId", "DeParticipanteUsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_gastos_LiquidacionId",
                table: "gastos",
                column: "LiquidacionId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_gastos_monto_total",
                table: "gastos",
                sql: """ "Monto" > 0 """);

            migrationBuilder.AddForeignKey(
                name: "FK_division_gastos_gastos_GastoId",
                table: "division_gastos",
                column: "GastoId",
                principalTable: "gastos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gastos_liquidaciones_LiquidacionId",
                table: "gastos",
                column: "LiquidacionId",
                principalTable: "liquidaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transferencias_liquidacion_liquidaciones_LiquidacionId",
                table: "transferencias_liquidacion",
                column: "LiquidacionId",
                principalTable: "liquidaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_transferencias_liquidacion_miembros_viaje_DeParticipanteVia~",
                table: "transferencias_liquidacion",
                columns: new[] { "DeParticipanteViajeId", "DeParticipanteUsuarioId" },
                principalTable: "miembros_viaje",
                principalColumns: new[] { "viaje_id", "usuario_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
