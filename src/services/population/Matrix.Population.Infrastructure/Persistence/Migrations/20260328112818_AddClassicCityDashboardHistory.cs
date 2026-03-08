using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassicCityDashboardHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationActivityEvents",
                columns: table => new
                {
                    ActivityEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentDate = table.Column<DateTime>(type: "date", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Summary = table.Column<string>(type: "character varying(640)", maxLength: 640, nullable: false),
                    PrimaryResidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SecondaryResidentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationActivityEvents", x => x.ActivityEventId);
                });

            migrationBuilder.CreateTable(
                name: "CityPopulationDailySummarySnapshots",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "date", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HouseholdCount = table.Column<int>(type: "integer", nullable: false),
                    HousedHouseholdCount = table.Column<int>(type: "integer", nullable: false),
                    HomelessHouseholdCount = table.Column<int>(type: "integer", nullable: false),
                    ResidentCount = table.Column<int>(type: "integer", nullable: false),
                    DeceasedCount = table.Column<int>(type: "integer", nullable: false),
                    HousedResidentCount = table.Column<int>(type: "integer", nullable: false),
                    HomelessResidentCount = table.Column<int>(type: "integer", nullable: false),
                    ChildCount = table.Column<int>(type: "integer", nullable: false),
                    YouthCount = table.Column<int>(type: "integer", nullable: false),
                    AdultCount = table.Column<int>(type: "integer", nullable: false),
                    SeniorCount = table.Column<int>(type: "integer", nullable: false),
                    EmployedCount = table.Column<int>(type: "integer", nullable: false),
                    StudentCount = table.Column<int>(type: "integer", nullable: false),
                    UnemployedCount = table.Column<int>(type: "integer", nullable: false),
                    RetiredCount = table.Column<int>(type: "integer", nullable: false),
                    AverageHealth = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AverageHappiness = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AverageEnergy = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AverageStress = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AverageSocialNeed = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationDailySummarySnapshots", x => new { x.CityId, x.SnapshotDate });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationActivityEvents_CityId_CurrentDate",
                table: "CityPopulationActivityEvents",
                columns: new[] { "CityId", "CurrentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationActivityEvents_CityId_OccurredAtUtc",
                table: "CityPopulationActivityEvents",
                columns: new[] { "CityId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationActivityEvents_EventType",
                table: "CityPopulationActivityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationDailySummarySnapshots_CityId_UpdatedAtUtc",
                table: "CityPopulationDailySummarySnapshots",
                columns: new[] { "CityId", "UpdatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationActivityEvents");

            migrationBuilder.DropTable(
                name: "CityPopulationDailySummarySnapshots");
        }
    }
}
