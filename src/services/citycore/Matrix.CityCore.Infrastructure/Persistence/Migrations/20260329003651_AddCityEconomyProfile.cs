using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityEconomyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationEconomyProfile",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationEconomyProfile",
                table: "Cities");
        }
    }
}
