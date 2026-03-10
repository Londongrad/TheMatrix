using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityBudgetOperatingExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "total_city_expenses_amount",
                table: "City_Budget",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_city_expenses_amount",
                table: "City_Budget");
        }
    }
}
