using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaterDistributionInfrastructureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WaterCoverageIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionBacklogIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionCrewReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "WaterDistributionEmergencyModeEnabled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionFailureRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionIncidentPressureIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WaterDistributionKind",
                table: "CityEnvironmentalConditions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionLoadIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionNetworkIntegrityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionPumpReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionServiceQualityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDistributionTreatmentCapacityIndex",
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
                name: "WaterCoverageIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionBacklogIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionCrewReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionEmergencyModeEnabled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionFailureRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionIncidentPressureIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionKind",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionLoadIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionNetworkIntegrityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionPumpReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionServiceQualityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "WaterDistributionTreatmentCapacityIndex",
                table: "CityEnvironmentalConditions");
        }
    }
}
