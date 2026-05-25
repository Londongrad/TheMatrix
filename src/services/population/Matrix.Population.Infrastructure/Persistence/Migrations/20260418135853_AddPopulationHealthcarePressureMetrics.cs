using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPopulationHealthcarePressureMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveIllnessCount",
                table: "CityPopulationSummaryProjections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MedicalLoadIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySupportIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SevereIllnessCount",
                table: "CityPopulationSummaryProjections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TriagePressureIndex",
                table: "CityPopulationSummaryProjections",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveIllnessCount",
                table: "CityPopulationDailySummarySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MedicalLoadIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecoverySupportIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SevereIllnessCount",
                table: "CityPopulationDailySummarySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TriagePressureIndex",
                table: "CityPopulationDailySummarySnapshots",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveIllnessCount",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "MedicalLoadIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "RecoverySupportIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "SevereIllnessCount",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "TriagePressureIndex",
                table: "CityPopulationSummaryProjections");

            migrationBuilder.DropColumn(
                name: "ActiveIllnessCount",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "MedicalLoadIndex",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "RecoverySupportIndex",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "SevereIllnessCount",
                table: "CityPopulationDailySummarySnapshots");

            migrationBuilder.DropColumn(
                name: "TriagePressureIndex",
                table: "CityPopulationDailySummarySnapshots");
        }
    }
}
