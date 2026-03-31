using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingOperationalWorkState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingDrainageMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingDrainageMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingDrainageMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingDrainageMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingHeatingMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingHeatingMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingHeatingMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingHeatingMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingPowerDistributionMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingPowerDistributionMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingPowerDistributionMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingPowerDistributionMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingRoadAccessMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingRoadAccessMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingRoadAccessMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingRoadAccessMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingSanitationMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingSanitationMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingSanitationMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingSanitationMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingSnowRemovalMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingSnowRemovalMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingSnowRemovalMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingSnowRemovalMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingUtilityIncidentResponseFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingUtilityIncidentResponseIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingUtilityIncidentResponseIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingUtilityIncidentResponseReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PendingWaterDistributionMaintenanceFocus",
                table: "CityEnvironmentalConditions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PendingWaterDistributionMaintenanceIntensity",
                table: "CityEnvironmentalConditions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PendingWaterDistributionMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "PendingWaterDistributionMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingDrainageMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingDrainageMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingDrainageMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingDrainageMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingHeatingMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingHeatingMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingHeatingMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingHeatingMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingPowerDistributionMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingPowerDistributionMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingPowerDistributionMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingPowerDistributionMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingRoadAccessMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingRoadAccessMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingRoadAccessMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingRoadAccessMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSanitationMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSanitationMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSanitationMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSanitationMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSnowRemovalMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSnowRemovalMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSnowRemovalMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSnowRemovalMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingUtilityIncidentResponseFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingUtilityIncidentResponseIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingUtilityIncidentResponseIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingUtilityIncidentResponseReadyAtTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingWaterDistributionMaintenanceFocus",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingWaterDistributionMaintenanceIntensity",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingWaterDistributionMaintenanceIsScheduled",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingWaterDistributionMaintenanceReadyAtTickId",
                table: "CityEnvironmentalConditions");
        }
    }
}
