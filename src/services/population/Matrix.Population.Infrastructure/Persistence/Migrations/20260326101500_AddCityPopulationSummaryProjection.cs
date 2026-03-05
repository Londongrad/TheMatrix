using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityPopulationSummaryProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationSummaryProjections",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentDate = table.Column<DateTime>(type: "date", nullable: false),
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
                    table.PrimaryKey("PK_CityPopulationSummaryProjections", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationSummaryProjections_UpdatedAtUtc",
                table: "CityPopulationSummaryProjections",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationSummaryProjections");
        }
    }
}
