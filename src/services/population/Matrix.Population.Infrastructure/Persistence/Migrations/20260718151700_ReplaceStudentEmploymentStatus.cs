using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PopulationDbContext))]
    [Migration("20260718151700_ReplaceStudentEmploymentStatus")]
    public sealed class ReplaceStudentEmploymentStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Persons"
                SET "EmploymentStatus" = 'None'
                WHERE "EmploymentStatus" = 'Student';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
