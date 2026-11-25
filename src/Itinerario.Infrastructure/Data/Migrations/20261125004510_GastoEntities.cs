using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GastoEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "liquidaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViajeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Moneda = table.Column<string>(type: "char(3)", nullable: false),
                    RealizadaPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaLiquidacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_liquidaciones", x => x.Id);
                    table.UniqueConstraint("AK_liquidaciones_Id_ViajeId", x => new { x.Id, x.ViajeId });
                    table.CheckConstraint("ck_liquidaciones_moneda", "length(trim(\"Moneda\"::text)) = 3");
                    table.ForeignKey(
                        name: "FK_liquidaciones_viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gastos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViajeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PagadoPorMiembroId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistradoPorMiembroId = table.Column<Guid>(type: "uuid", nullable: false),
                    LiquidacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Concepto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "char(3)", nullable: false),
                    FechaGasto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false, defaultValue: 1u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gastos", x => x.Id);
                    table.UniqueConstraint("AK_gastos_Id_ViajeId", x => new { x.Id, x.ViajeId });
                    table.CheckConstraint("ck_gastos_monto_total", " \"Monto\" > 0 ");
                    table.ForeignKey(
                        name: "FK_gastos_liquidaciones_LiquidacionId_ViajeId",
                        columns: x => new { x.LiquidacionId, x.ViajeId },
                        principalTable: "liquidaciones",
                        principalColumns: new[] { "Id", "ViajeId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gastos_miembros_viaje_ViajeId_PagadoPorMiembroId",
                        columns: x => new { x.ViajeId, x.PagadoPorMiembroId },
                        principalTable: "miembros_viaje",
                        principalColumns: new[] { "viaje_id", "usuario_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gastos_viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transferencias_liquidacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViajeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LiquidacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeParticipanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AParticipanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias_liquidacion", x => x.Id);
                    table.CheckConstraint("ck_transferencias_monto", " \"Monto\" > 0");
                    table.CheckConstraint("ck_transferencias_participantes", " \"DeParticipanteId\" <> \"AParticipanteId\" ");
                    table.ForeignKey(
                        name: "FK_transferencias_liquidacion_liquidaciones_LiquidacionId_Viaj~",
                        columns: x => new { x.LiquidacionId, x.ViajeId },
                        principalTable: "liquidaciones",
                        principalColumns: new[] { "Id", "ViajeId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transferencias_liquidacion_miembros_viaje_ViajeId_AParticip~",
                        columns: x => new { x.ViajeId, x.AParticipanteId },
                        principalTable: "miembros_viaje",
                        principalColumns: new[] { "viaje_id", "usuario_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencias_liquidacion_miembros_viaje_ViajeId_DePartici~",
                        columns: x => new { x.ViajeId, x.DeParticipanteId },
                        principalTable: "miembros_viaje",
                        principalColumns: new[] { "viaje_id", "usuario_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "division_gastos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ViajeId = table.Column<Guid>(type: "uuid", nullable: false),
                    GastoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MontoAsignado = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_division_gastos", x => x.Id);
                    table.CheckConstraint("ck_division_gastos_monto_asignado", " \"MontoAsignado\" > 0");
                    table.ForeignKey(
                        name: "FK_division_gastos_gastos_GastoId_ViajeId",
                        columns: x => new { x.GastoId, x.ViajeId },
                        principalTable: "gastos",
                        principalColumns: new[] { "Id", "ViajeId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_division_gastos_miembros_viaje_ViajeId_ParticipanteId",
                        columns: x => new { x.ViajeId, x.ParticipanteId },
                        principalTable: "miembros_viaje",
                        principalColumns: new[] { "viaje_id", "usuario_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_division_gastos_GastoId_ViajeId",
                table: "division_gastos",
                columns: new[] { "GastoId", "ViajeId" });

            migrationBuilder.CreateIndex(
                name: "ix_division_gastos_participante",
                table: "division_gastos",
                column: "ParticipanteId");

            migrationBuilder.CreateIndex(
                name: "IX_division_gastos_ViajeId_ParticipanteId",
                table: "division_gastos",
                columns: new[] { "ViajeId", "ParticipanteId" });

            migrationBuilder.CreateIndex(
                name: "uq_division_gasto_participante",
                table: "division_gastos",
                columns: new[] { "GastoId", "ParticipanteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gastos_LiquidacionId_ViajeId",
                table: "gastos",
                columns: new[] { "LiquidacionId", "ViajeId" });

            migrationBuilder.CreateIndex(
                name: "ix_gastos_pagado_por",
                table: "gastos",
                column: "PagadoPorMiembroId");

            migrationBuilder.CreateIndex(
                name: "ix_gastos_registrado_por",
                table: "gastos",
                column: "RegistradoPorMiembroId");

            migrationBuilder.CreateIndex(
                name: "ix_gastos_viaje_fecha",
                table: "gastos",
                columns: new[] { "ViajeId", "FechaGasto" });

            migrationBuilder.CreateIndex(
                name: "ix_gastos_viaje_liquidacion",
                table: "gastos",
                columns: new[] { "ViajeId", "LiquidacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_gastos_ViajeId_PagadoPorMiembroId",
                table: "gastos",
                columns: new[] { "ViajeId", "PagadoPorMiembroId" });

            migrationBuilder.CreateIndex(
                name: "ix_liquidaciones_realizada_por",
                table: "liquidaciones",
                column: "RealizadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "ix_liquidaciones_viaje_fecha",
                table: "liquidaciones",
                columns: new[] { "ViajeId", "FechaLiquidacion" });

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_a_participante",
                table: "transferencias_liquidacion",
                column: "AParticipanteId");

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_de_participante",
                table: "transferencias_liquidacion",
                column: "DeParticipanteId");

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_liquidacion",
                table: "transferencias_liquidacion",
                column: "LiquidacionId");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_liquidacion_LiquidacionId_ViajeId",
                table: "transferencias_liquidacion",
                columns: new[] { "LiquidacionId", "ViajeId" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_liquidacion_ViajeId_AParticipanteId",
                table: "transferencias_liquidacion",
                columns: new[] { "ViajeId", "AParticipanteId" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_liquidacion_ViajeId_DeParticipanteId",
                table: "transferencias_liquidacion",
                columns: new[] { "ViajeId", "DeParticipanteId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "division_gastos");

            migrationBuilder.DropTable(
                name: "transferencias_liquidacion");

            migrationBuilder.DropTable(
                name: "gastos");

            migrationBuilder.DropTable(
                name: "liquidaciones");
        }
    }
}
