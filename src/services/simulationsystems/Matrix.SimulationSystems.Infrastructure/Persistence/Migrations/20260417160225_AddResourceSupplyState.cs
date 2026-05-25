using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceSupplyState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResourceEffectiveAtUtc",
                table: "CityEnvironmentalConditions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceEmergencyWaterResupplyReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceEmergencyWaterShortageRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceEmergencyWaterStockLevelIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceFiltersResupplyReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceFiltersShortageRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceFiltersStockLevelIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceFuelResupplyReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceFuelShortageRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceFuelStockLevelIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceSparePartsResupplyReadinessIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceSparePartsShortageRiskIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceSparePartsStockLevelIndex",
                table: "CityEnvironmentalConditions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceSupplyStressIndex",
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
                name: "ResourceEffectiveAtUtc",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceEmergencyWaterResupplyReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceEmergencyWaterShortageRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceEmergencyWaterStockLevelIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceFiltersResupplyReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceFiltersShortageRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceFiltersStockLevelIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceFuelResupplyReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceFuelShortageRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceFuelStockLevelIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceSparePartsResupplyReadinessIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceSparePartsShortageRiskIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceSparePartsStockLevelIndex",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceSupplyStressIndex",
                table: "CityEnvironmentalConditions");
        }
    }
}
