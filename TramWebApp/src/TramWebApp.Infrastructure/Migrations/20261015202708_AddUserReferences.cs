using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TramWebApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_model_inferences_users_user_id",
                table: "model_inferences");

            migrationBuilder.DropForeignKey(
                name: "FK_tram_runs_users_created_by_user_id",
                table: "tram_runs");

            migrationBuilder.AddForeignKey(
                name: "fk_model_inferences_users_user_id",
                table: "model_inferences",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tram_runs_users_created_by",
                table: "tram_runs",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_model_inferences_users_user_id",
                table: "model_inferences");

            migrationBuilder.DropForeignKey(
                name: "fk_tram_runs_users_created_by",
                table: "tram_runs");

            migrationBuilder.AddForeignKey(
                name: "FK_model_inferences_users_user_id",
                table: "model_inferences",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tram_runs_users_created_by_user_id",
                table: "tram_runs",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
