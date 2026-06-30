using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EducationDbContext))]
    [Migration("20260630190700_AddStudentLifecycleRevision")]
    public sealed class AddStudentLifecycleRevision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_lifecycle_revision",
                table: "education_student_profiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_lifecycle_revision",
                table: "education_student_profiles");
        }
    }
}
