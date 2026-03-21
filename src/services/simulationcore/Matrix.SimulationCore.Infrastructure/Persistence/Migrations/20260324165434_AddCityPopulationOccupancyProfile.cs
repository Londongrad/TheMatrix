using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityPopulationOccupancyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationPopulationOccupancyProfile",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationPopulationOccupancyProfile",
                table: "Cities");
        }
    }
}
