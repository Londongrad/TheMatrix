using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityEmployerFinancialStressState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationEmployerFinancialStressStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkplaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecentGrossPayrollAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalanceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DistressScore = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    HasHiringFreeze = table.Column<bool>(type: "boolean", nullable: false),
                    HasLayoffPressure = table.Column<bool>(type: "boolean", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationEmployerFinancialStressStates", x => new { x.CityId, x.WorkplaceId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationEmployerFinancialStressStates_UpdatedAtUtc",
                table: "CityPopulationEmployerFinancialStressStates",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationEmployerFinancialStressStates");
        }
    }
}
