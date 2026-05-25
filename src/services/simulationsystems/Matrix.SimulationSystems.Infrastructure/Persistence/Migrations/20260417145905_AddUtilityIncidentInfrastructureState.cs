using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUtilityIncidentInfrastructureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UtilityContinuityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsBacklogIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsDispatchReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UtilityIncidentsEmergencyModeEnabled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsFailureRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsFieldCoordinationIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsIncidentQueuePressureIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UtilityIncidentsKind",
                table: "CityEnvironmentalConditions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsLoadIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsRestorationCoverageIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsServiceQualityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UtilityIncidentsSpareCapacityIndex",
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
                name: "UtilityContinuityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsBacklogIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsDispatchReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsEmergencyModeEnabled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsFailureRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsFieldCoordinationIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsIncidentQueuePressureIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsKind",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsLoadIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsRestorationCoverageIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsServiceQualityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "UtilityIncidentsSpareCapacityIndex",
                table: "CityEnvironmentalConditions");
        }
    }
}
