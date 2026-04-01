using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPopulationParticipationMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StudentAttendanceIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkforceAttendanceIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkforceProductivityIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StudentAttendanceIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkforceAttendanceIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkforceProductivityIndex",
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
                name: "StudentAttendanceIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "WorkforceAttendanceIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "WorkforceProductivityIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "StudentAttendanceIndex",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "WorkforceAttendanceIndex",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "WorkforceProductivityIndex",
                table: "CityPopulationDailySummarySnapshots");
        }
    }
}
