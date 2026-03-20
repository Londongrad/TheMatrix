using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCityHouseholdFinancialStressLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArrearsObligationCount",
                table: "CityPopulationHouseholdFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EvictionEligibleCount",
                table: "CityPopulationHouseholdFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EvictionNoticeCount",
                table: "CityPopulationHouseholdFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OldestOverdueAgeDays",
                table: "CityPopulationHouseholdFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceCutoffCount",
                table: "CityPopulationHouseholdFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrearsObligationCount",
                table: "CityPopulationHouseholdFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "EvictionEligibleCount",
                table: "CityPopulationHouseholdFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "EvictionNoticeCount",
                table: "CityPopulationHouseholdFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "OldestOverdueAgeDays",
                table: "CityPopulationHouseholdFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "ServiceCutoffCount",
                table: "CityPopulationHouseholdFinancialStressStates");
        }
    }
}
