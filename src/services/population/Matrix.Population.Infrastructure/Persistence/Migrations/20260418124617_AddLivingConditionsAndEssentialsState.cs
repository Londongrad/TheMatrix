using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLivingConditionsAndEssentialsState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationEssentialsStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyStressIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    EmergencyRationingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FoodStockLevelIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    FoodShortageRiskIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    MedicineStockLevelIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    MedicineShortageRiskIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    EmergencyWaterStockLevelIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    EmergencyWaterShortageRiskIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    EffectiveTickId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationEssentialsStates", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "CityPopulationLivingConditionsStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FloodingIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    RoadAccessibilityIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    PowerCoverageIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    UtilityContinuityIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    HeatingCoverageIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    WaterCoverageIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    SanitationCoverageIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    EffectiveTickId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationLivingConditionsStates", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationEssentialsStates_EffectiveTickId",
                table: "CityPopulationEssentialsStates",
                column: "EffectiveTickId");

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationEssentialsStates_UpdatedAtUtc",
                table: "CityPopulationEssentialsStates",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationLivingConditionsStates_EffectiveTickId",
                table: "CityPopulationLivingConditionsStates",
                column: "EffectiveTickId");

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationLivingConditionsStates_UpdatedAtUtc",
                table: "CityPopulationLivingConditionsStates",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationEssentialsStates");

            migrationBuilder.DropTable(
                name: "CityPopulationLivingConditionsStates");
        }
    }
}
