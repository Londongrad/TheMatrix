using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "unit_code",
                table: "City_Budget",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unit_display_name",
                table: "City_Budget",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unit_kind",
                table: "City_Budget",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unit_symbol",
                table: "City_Budget",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unit_code",
                table: "City_Budget");

            migrationBuilder.DropColumn(
                name: "unit_display_name",
                table: "City_Budget");

            migrationBuilder.DropColumn(
                name: "unit_kind",
                table: "City_Budget");

            migrationBuilder.DropColumn(
                name: "unit_symbol",
                table: "City_Budget");
        }
    }
}
