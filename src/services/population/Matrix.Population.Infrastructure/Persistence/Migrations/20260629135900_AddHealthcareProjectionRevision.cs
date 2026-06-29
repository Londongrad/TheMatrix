using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PopulationDbContext))]
    [Migration("20260629135900_AddHealthcareProjectionRevision")]
    public sealed class AddHealthcareProjectionRevision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastHealthcareRevision",
                table: "Persons",
                type: "bigint",
                nullable: false,
                defaultValue: -1L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHealthcareRevision",
                table: "Persons");
        }
    }
}
