using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalBudgetPressureState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureBalance",
                table: "CityStockpiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BudgetPressureEffectiveAtUtc",
                table: "CityStockpiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureIndex",
                table: "CityStockpiles",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureMunicipalOperationsExpenses",
                table: "CityStockpiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetPressureBalance",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureEffectiveAtUtc",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureIndex",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureMunicipalOperationsExpenses",
                table: "CityStockpiles");
        }
    }
}
