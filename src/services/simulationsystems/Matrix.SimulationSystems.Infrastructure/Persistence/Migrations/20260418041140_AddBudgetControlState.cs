using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetControlState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureGeneralAuthorizationLevel",
                table: "CityEnvironmentalConditions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureGeneralAvailableAmount",
                table: "CityEnvironmentalConditions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureHealthcareAuthorizationLevel",
                table: "CityEnvironmentalConditions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureHealthcareAvailableAmount",
                table: "CityEnvironmentalConditions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureInfrastructureAuthorizationLevel",
                table: "CityEnvironmentalConditions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureInfrastructureAvailableAmount",
                table: "CityEnvironmentalConditions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BudgetPressureOperationsAuthorizationLevel",
                table: "CityEnvironmentalConditions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetPressureOperationsAvailableAmount",
                table: "CityEnvironmentalConditions",
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
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureGeneralAvailableAmount",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureHealthcareAuthorizationLevel",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureHealthcareAvailableAmount",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureInfrastructureAuthorizationLevel",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureInfrastructureAvailableAmount",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureOperationsAuthorizationLevel",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "BudgetPressureOperationsAvailableAmount",
                table: "CityEnvironmentalConditions");
        }
    }
}
