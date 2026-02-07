using Matrix.CityCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.CityCore.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CityCoreDbContext))]
    [Migration("20260313213000_AddCitySimulationKind")]
    public partial class AddCitySimulationKind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SimulationKind",
                table: "Cities",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Cities"
                SET "SimulationKind" = 1
                WHERE "SimulationKind" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "SimulationKind",
                table: "Cities",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimulationKind",
                table: "Cities");
        }
    }
}
