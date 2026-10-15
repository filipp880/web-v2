using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TramWebApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "planned_route",
                table: "tram_runs",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "planned_route",
                table: "tram_runs");
        }
    }
}
