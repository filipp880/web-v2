using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TramWebApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tram_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tram_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_tram_runs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "model_inferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tram_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inference_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    input_controller_position = table.Column<double>(type: "double precision", nullable: false),
                    input_front_speed = table.Column<double>(type: "double precision", nullable: false),
                    input_rear_speed = table.Column<double>(type: "double precision", nullable: false),
                    predicted_speed = table.Column<double>(type: "double precision", nullable: false),
                    predicted_position = table.Column<double>(type: "double precision", nullable: false),
                    actual_speed = table.Column<double>(type: "double precision", nullable: true),
                    actual_position = table.Column<double>(type: "double precision", nullable: true),
                    user_adjusted_speed = table.Column<double>(type: "double precision", nullable: true),
                    user_adjusted_position = table.Column<double>(type: "double precision", nullable: true),
                    is_user_intervention = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    error_speed = table.Column<double>(type: "double precision", nullable: true),
                    error_position = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_inferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_model_inferences_tram_runs_tram_run_id",
                        column: x => x.tram_run_id,
                        principalTable: "tram_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_model_inferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tram_telemetry_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tram_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<double>(type: "double precision", nullable: false),
                    controller_handle_position = table.Column<double>(type: "double precision", nullable: false),
                    front_bogie_speed = table.Column<double>(type: "double precision", nullable: false),
                    rear_bogie_speed = table.Column<double>(type: "double precision", nullable: false),
                    gps_latitude = table.Column<double>(type: "double precision", nullable: true),
                    gps_longitude = table.Column<double>(type: "double precision", nullable: true),
                    gps_speed = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tram_telemetry_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_tram_telemetry_points_tram_runs_tram_run_id",
                        column: x => x.tram_run_id,
                        principalTable: "tram_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_model_inferences_inference_time",
                table: "model_inferences",
                column: "inference_time");

            migrationBuilder.CreateIndex(
                name: "ix_model_inferences_tram_run_id",
                table: "model_inferences",
                column: "tram_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_model_inferences_user_id",
                table: "model_inferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tram_runs_created_by_user_id",
                table: "tram_runs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tram_runs_is_active",
                table: "tram_runs",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_tram_telemetry_run_sequence",
                table: "tram_telemetry_points",
                columns: new[] { "tram_run_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tram_telemetry_tram_run_id",
                table: "tram_telemetry_points",
                column: "tram_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "model_inferences");

            migrationBuilder.DropTable(
                name: "tram_telemetry_points");

            migrationBuilder.DropTable(
                name: "tram_runs");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
