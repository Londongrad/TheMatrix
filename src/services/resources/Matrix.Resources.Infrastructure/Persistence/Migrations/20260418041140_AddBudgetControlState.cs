using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetControlState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureGeneralAuthorizationLevel",
                table: "CityStockpiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureGeneralAvailableAmount",
                table: "CityStockpiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureHealthcareAuthorizationLevel",
                table: "CityStockpiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureHealthcareAvailableAmount",
                table: "CityStockpiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureInfrastructureAuthorizationLevel",
                table: "CityStockpiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureInfrastructureAvailableAmount",
                table: "CityStockpiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureOperationsAuthorizationLevel",
                table: "CityStockpiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureOperationsAvailableAmount",
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
                name: "BudgetPressureGeneralAuthorizationLevel",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureGeneralAvailableAmount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureHealthcareAuthorizationLevel",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureHealthcareAvailableAmount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureInfrastructureAuthorizationLevel",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureInfrastructureAvailableAmount",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureOperationsAuthorizationLevel",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "BudgetPressureOperationsAvailableAmount",
                table: "CityStockpiles");
        }
    }
}
