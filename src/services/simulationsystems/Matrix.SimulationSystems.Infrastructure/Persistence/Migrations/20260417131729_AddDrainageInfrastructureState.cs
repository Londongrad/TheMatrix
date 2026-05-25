using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDrainageInfrastructureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DrainageBlockageIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DrainageCrewReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DrainageEmergencyModeEnabled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DrainageIncidentPressureIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DrainageNetworkIntegrityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DrainagePumpCapacityIndex",
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
                name: "DrainageBlockageIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "DrainageCrewReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "DrainageEmergencyModeEnabled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "DrainageIncidentPressureIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "DrainageNetworkIntegrityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "DrainagePumpCapacityIndex",
                table: "CityEnvironmentalConditions");
        }
    }
}
