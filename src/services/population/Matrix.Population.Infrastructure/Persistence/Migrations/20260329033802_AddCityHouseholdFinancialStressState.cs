using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHouseholdFinancialStressState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationHouseholdFinancialStressStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverdueObligationCount = table.Column<int>(type: "integer", nullable: false),
                    OverdueRentCount = table.Column<int>(type: "integer", nullable: false),
                    OverdueUtilityCount = table.Column<int>(type: "integer", nullable: false),
                    TotalOverdueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DistressScore = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationHouseholdFinancialStressStates", x => new { x.CityId, x.HouseholdId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationHouseholdFinancialStressStates_UpdatedAtUtc",
                table: "CityPopulationHouseholdFinancialStressStates",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationHouseholdFinancialStressStates");
        }
    }
}
