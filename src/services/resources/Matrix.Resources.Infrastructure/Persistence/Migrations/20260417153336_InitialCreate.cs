using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityStockpiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FuelKind = table.Column<string>(type: "text", nullable: false),
                    FuelStockLevelIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FuelDemandPressureIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FuelResupplyReadinessIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FuelShortageRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FoodKind = table.Column<string>(type: "text", nullable: false),
                    FoodStockLevelIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FoodDemandPressureIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FoodResupplyReadinessIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FoodShortageRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MedicineKind = table.Column<string>(type: "text", nullable: false),
                    MedicineStockLevelIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MedicineDemandPressureIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MedicineResupplyReadinessIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MedicineShortageRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SparePartsKind = table.Column<string>(type: "text", nullable: false),
                    SparePartsStockLevelIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SparePartsDemandPressureIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SparePartsResupplyReadinessIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SparePartsShortageRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FiltersKind = table.Column<string>(type: "text", nullable: false),
                    FiltersStockLevelIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FiltersDemandPressureIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FiltersResupplyReadinessIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FiltersShortageRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EmergencyWaterKind = table.Column<string>(type: "text", nullable: false),
                    EmergencyWaterStockLevelIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EmergencyWaterDemandPressureIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EmergencyWaterResupplyReadinessIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EmergencyWaterShortageRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SupplyStressIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EmergencyRationingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityStockpiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityStockpiles");
        }
    }
}
