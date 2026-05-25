using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSanitationInfrastructureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SanitationBacklogIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationCoverageIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationCrewReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SanitationEmergencyModeEnabled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationFailureRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationIncidentPressureIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SanitationKind",
                table: "CityEnvironmentalConditions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationLoadIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationNetworkIntegrityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationOverflowControlIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationServiceQualityIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SanitationTreatmentStabilityIndex",
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
                name: "SanitationBacklogIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationCoverageIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationCrewReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationEmergencyModeEnabled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationFailureRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationIncidentPressureIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationKind",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationLoadIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationNetworkIntegrityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationOverflowControlIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationServiceQualityIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "SanitationTreatmentStabilityIndex",
                table: "CityEnvironmentalConditions");
        }
    }
}
