using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCityBudgetEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "City_Budget",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_tax_income_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_income_tax_income_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_sales_tax_income_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_retail_turnover_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_gross_payroll_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_net_payroll_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Budget", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "City_Budget_Settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tick_id = table.Column<long>(type: "bigint", nullable: false),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    settled_days = table.Column<int>(type: "integer", nullable: false),
                    household_count = table.Column<int>(type: "integer", nullable: false),
                    resident_count = table.Column<int>(type: "integer", nullable: false),
                    gross_payroll_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    income_tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    net_payroll_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    retail_turnover_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    retail_tax_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    housing_spend_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    correlation_id = table.Column<string>(type: "text", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City_Budget_Settlements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_city_id",
                table: "City_Budget",
                column: "city_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Settlements_city_id_tick_id",
                table: "City_Budget_Settlements",
                columns: new[] { "city_id", "tick_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Budget_Settlements_correlation_id",
                table: "City_Budget_Settlements",
                column: "correlation_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "City_Budget");

            migrationBuilder.DropTable(
                name: "City_Budget_Settlements");
        }
    }
}
