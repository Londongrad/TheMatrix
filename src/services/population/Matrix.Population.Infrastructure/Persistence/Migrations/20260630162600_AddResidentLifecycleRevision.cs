using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PopulationDbContext))]
    [Migration("20260630162600_AddResidentLifecycleRevision")]
    public sealed class AddResidentLifecycleRevision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LifecycleRevision",
                table: "Persons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LifecycleRevision",
                table: "Persons");
        }
    }
}
