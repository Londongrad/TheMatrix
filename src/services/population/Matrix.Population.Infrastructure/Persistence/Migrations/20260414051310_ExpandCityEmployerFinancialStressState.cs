using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCityEmployerFinancialStressState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecentGrossPayrollAmount",
                table: "CityPopulationEmployerFinancialStressStates",
                newName: "RequestedGrossPayrollAmount");

            migrationBuilder.AddColumn<int>(
                name: "FailedPayrollCount",
                table: "CityPopulationEmployerFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MissedGrossPayrollAmount",
                table: "CityPopulationEmployerFinancialStressStates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidGrossPayrollAmount",
                table: "CityPopulationEmployerFinancialStressStates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PartialPayrollCount",
                table: "CityPopulationEmployerFinancialStressStates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PayrollFulfillmentRatio",
                table: "CityPopulationEmployerFinancialStressStates",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "CityPopulationEmployerFinancialStressStates"
                SET "PaidGrossPayrollAmount" = "RequestedGrossPayrollAmount",
                    "MissedGrossPayrollAmount" = 0,
                    "PayrollFulfillmentRatio" = 1,
                    "FailedPayrollCount" = 0,
                    "PartialPayrollCount" = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedPayrollCount",
                table: "CityPopulationEmployerFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "MissedGrossPayrollAmount",
                table: "CityPopulationEmployerFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "PaidGrossPayrollAmount",
                table: "CityPopulationEmployerFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "PartialPayrollCount",
                table: "CityPopulationEmployerFinancialStressStates");

            migrationBuilder.DropColumn(
                name: "PayrollFulfillmentRatio",
                table: "CityPopulationEmployerFinancialStressStates");

            migrationBuilder.RenameColumn(
                name: "RequestedGrossPayrollAmount",
                table: "CityPopulationEmployerFinancialStressStates",
                newName: "RecentGrossPayrollAmount");
        }
    }
}
