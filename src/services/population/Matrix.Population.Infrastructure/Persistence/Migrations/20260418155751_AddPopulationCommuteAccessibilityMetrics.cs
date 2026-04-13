using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPopulationCommuteAccessibilityMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StudentCommuteAccessibilityIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkforceCommuteAccessibilityIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StudentCommuteAccessibilityIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkforceCommuteAccessibilityIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentCommuteAccessibilityIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "WorkforceCommuteAccessibilityIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "StudentCommuteAccessibilityIndex",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "WorkforceCommuteAccessibilityIndex",
                table: "CityPopulationDailySummarySnapshots");
        }
    }
}
