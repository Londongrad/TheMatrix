using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingOperationFocusDistricts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PendingDrainageMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingHeatingMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingPowerDistributionMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingRoadAccessMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingSanitationMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingSnowRemovalMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingUtilityIncidentResponseFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingWaterDistributionMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingDrainageMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingHeatingMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingPowerDistributionMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingRoadAccessMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSanitationMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingSnowRemovalMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingUtilityIncidentResponseFocusDistrictId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "PendingWaterDistributionMaintenanceFocusDistrictId",
                table: "CityEnvironmentalConditions");
        }
    }
}
