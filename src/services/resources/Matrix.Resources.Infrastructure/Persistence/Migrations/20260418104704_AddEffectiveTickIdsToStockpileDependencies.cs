using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Resources.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEffectiveTickIdsToStockpileDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BudgetPressureEffectiveTickId",
                table: "CityStockpiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SystemsDemandEffectiveTickId",
                table: "CityStockpiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetPressureEffectiveTickId",
                table: "CityStockpiles");

            migrationBuilder.DropColumn(
                name: "SystemsDemandEffectiveTickId",
                table: "CityStockpiles");
        }
    }
}
