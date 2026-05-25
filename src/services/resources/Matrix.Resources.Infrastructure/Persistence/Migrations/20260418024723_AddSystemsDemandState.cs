using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemsDemandState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SystemsDemandEffectiveAtUtc",
                table: "CityStockpiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "SystemsDemandEmergencyWaterDemandPressureIndex",
                table: "CityStockpiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SystemsDemandFiltersDemandPressureIndex",
                table: "CityStockpiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SystemsDemandFuelDemandPressureIndex",
                table: "CityStockpiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SystemsDemandOverallDemandPressureIndex",
                table: "CityStockpiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SystemsDemandSparePartsDemandPressureIndex",
                table: "CityStockpiles",
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
                name: "SystemsDemandEffectiveAtUtc",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "SystemsDemandEmergencyWaterDemandPressureIndex",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "SystemsDemandFiltersDemandPressureIndex",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "SystemsDemandFuelDemandPressureIndex",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "SystemsDemandOverallDemandPressureIndex",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "SystemsDemandSparePartsDemandPressureIndex",
                table: "CityStockpiles");
        }
    }
}
