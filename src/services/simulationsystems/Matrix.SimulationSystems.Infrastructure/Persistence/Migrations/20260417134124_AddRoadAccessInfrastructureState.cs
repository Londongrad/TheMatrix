using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadAccessInfrastructureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RoadAccessCorridorAvailabilityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RoadAccessCrewReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "RoadAccessEmergencyModeEnabled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RoadAccessIncidentPressureIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RoadAccessSurfaceIntegrityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RoadAccessTrafficControlReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoadAccessCorridorAvailabilityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "RoadAccessCrewReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "RoadAccessEmergencyModeEnabled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "RoadAccessIncidentPressureIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "RoadAccessSurfaceIntegrityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "RoadAccessTrafficControlReadinessIndex",
                table: "CityEnvironmentalConditions");
        }
    }
}
