using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEffectiveTickIdsToExternalSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BudgetPressureEffectiveTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ResourceEffectiveTickId",
                table: "CityEnvironmentalConditions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetPressureEffectiveTickId",
                table: "CityEnvironmentalConditions");

            migrationBuilder.DropColumn(
                name: "ResourceEffectiveTickId",
                table: "CityEnvironmentalConditions");
        }
    }
}
