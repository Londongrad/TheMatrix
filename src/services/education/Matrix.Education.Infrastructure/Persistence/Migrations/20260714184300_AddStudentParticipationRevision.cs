using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentParticipationRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "participation_revision",
                table: "education_student_profiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "participation_revision",
                table: "education_student_profiles");
        }
    }
}
