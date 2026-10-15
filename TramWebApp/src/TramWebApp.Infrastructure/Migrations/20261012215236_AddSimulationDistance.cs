using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TramWebApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "distance_meters",
                table: "tram_telemetry_points",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "distance_meters",
                table: "tram_telemetry_points");
        }
    }
}
