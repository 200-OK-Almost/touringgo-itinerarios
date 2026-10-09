using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itinerario.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "viajes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_viaje = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false, defaultValue: "Borrador"),
                    barrio = table.Column<string>(type: "text", nullable: false),
                    alcance_temporal = table.Column<string>(type: "text", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: true),
                    nivel_presupuesto = table.Column<string>(type: "text", nullable: false),
                    ritmo_caminata = table.Column<string>(type: "text", nullable: false),
                    limite_cuadras_entre_paradas = table.Column<short>(type: "smallint", nullable: true),
                    tipo_punto_partida = table.Column<string>(type: "text", nullable: false),
                    lugar_partida_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nombre_punto_partida = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    direccion_partida = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    latitud_partida = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    longitud_partida = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    codigo_invitacion = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    creado_por_usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_actualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_viajes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "etiquetas_viaje",
                columns: table => new
                {
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etiquetas_viaje", x => new { x.viaje_id, x.categoria_id });
                    table.ForeignKey(
                        name: "FK_etiquetas_viaje_viajes_viaje_id",
                        column: x => x.viaje_id,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itinerarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_dia = table.Column<short>(type: "smallint", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    origen_tipo = table.Column<string>(type: "text", nullable: false),
                    origen_descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    origen_latitud = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    origen_longitud = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_actualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itinerarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_itinerarios_viajes_viaje_id",
                        column: x => x.viaje_id,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "miembros_viaje",
                columns: table => new
                {
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    fecha_incorporacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_miembros_viaje", x => new { x.viaje_id, x.usuario_id });
                    table.ForeignKey(
                        name: "FK_miembros_viaje_viajes_viaje_id",
                        column: x => x.viaje_id,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "parametros_familia",
                columns: table => new
                {
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad_acompanantes = table.Column<short>(type: "smallint", nullable: false),
                    hay_ninos = table.Column<bool>(type: "boolean", nullable: false),
                    cantidad_ninos = table.Column<short>(type: "smallint", nullable: false),
                    movilidad_reducida = table.Column<bool>(type: "boolean", nullable: false),
                    requiere_ritmo_bajo = table.Column<bool>(type: "boolean", nullable: false),
                    requiere_menu_infantil = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parametros_familia", x => x.viaje_id);
                    table.ForeignKey(
                        name: "FK_parametros_familia_viajes_viaje_id",
                        column: x => x.viaje_id,
                        principalTable: "viajes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paradas_itinerario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    itinerario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lugar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    turno = table.Column<string>(type: "text", nullable: false),
                    orden_parada = table.Column<short>(type: "smallint", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    duracion_minutos = table.Column<short>(type: "smallint", nullable: true),
                    distancia_metros_desde_anterior = table.Column<int>(type: "integer", nullable: true),
                    cuadras_desde_anterior = table.Column<short>(type: "smallint", nullable: true),
                    minutos_a_pie_desde_anterior = table.Column<short>(type: "smallint", nullable: true),
                    origen_parada = table.Column<string>(type: "text", nullable: false),
                    sugerencia_ia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    notas_personales = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paradas_itinerario", x => x.id);
                    table.ForeignKey(
                        name: "FK_paradas_itinerario_itinerarios_itinerario_id",
                        column: x => x.itinerario_id,
                        principalTable: "itinerarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itinerarios_viaje_id_numero_dia",
                table: "itinerarios",
                columns: new[] { "viaje_id", "numero_dia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_miembros_viaje_usuario_id",
                table: "miembros_viaje",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_paradas_itinerario_itinerario_id_turno_orden_parada",
                table: "paradas_itinerario",
                columns: new[] { "itinerario_id", "turno", "orden_parada" });

            migrationBuilder.CreateIndex(
                name: "IX_viajes_codigo_invitacion",
                table: "viajes",
                column: "codigo_invitacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_viajes_creado_por_usuario_id",
                table: "viajes",
                column: "creado_por_usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etiquetas_viaje");

            migrationBuilder.DropTable(
                name: "miembros_viaje");

            migrationBuilder.DropTable(
                name: "paradas_itinerario");

            migrationBuilder.DropTable(
                name: "parametros_familia");

            migrationBuilder.DropTable(
                name: "itinerarios");

            migrationBuilder.DropTable(
                name: "viajes");
        }
    }
}
