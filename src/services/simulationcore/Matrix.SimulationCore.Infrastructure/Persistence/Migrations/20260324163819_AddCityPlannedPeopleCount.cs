using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityPlannedPeopleCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationPlannedPeopleCount",
                table: "Cities",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenerationPlannedPeopleCount",
                table: "Cities");
        }
    }
}
