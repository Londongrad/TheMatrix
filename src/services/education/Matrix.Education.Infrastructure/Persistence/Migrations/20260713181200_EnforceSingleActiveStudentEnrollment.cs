using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveStudentEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_education_enrollments_active_stage",
                table: "education_enrollments");

            migrationBuilder.CreateIndex(
                name: "ux_education_enrollments_active_resident",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "resident_id" },
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_education_enrollments_active_resident",
                table: "education_enrollments");

            migrationBuilder.CreateIndex(
                name: "ux_education_enrollments_active_stage",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "resident_id", "stage" },
                unique: true,
                filter: "status = 'Active'");
        }
    }
}
