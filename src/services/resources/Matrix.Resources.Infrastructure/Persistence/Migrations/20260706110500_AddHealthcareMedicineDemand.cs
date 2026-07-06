using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthcareMedicineDemand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HealthcareDemandAcuteCareDeliveryCount",
                table: "CityStockpiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HealthcareDemandCareDate",
                table: "CityStockpiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthcareDemandEmergencyCareDeliveryCount",
                table: "CityStockpiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HealthcareDemandMedicineLoadIndex",
                table: "CityStockpiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HealthcareDemandObservedAtUtc",
                table: "CityStockpiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthcareDemandProcessedPatientCount",
                table: "CityStockpiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HealthcareDemandRoutineCareDeliveryCount",
                table: "CityStockpiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "HealthcareDemandSourceRevision",
                table: "CityStockpiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HealthcareDemandUrgentCareDeliveryCount",
                table: "CityStockpiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthcareDemandAcuteCareDeliveryCount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandCareDate",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandEmergencyCareDeliveryCount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandMedicineLoadIndex",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandObservedAtUtc",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandProcessedPatientCount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandRoutineCareDeliveryCount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandSourceRevision",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "HealthcareDemandUrgentCareDeliveryCount",
                table: "CityStockpiles");
        }
    }
}
