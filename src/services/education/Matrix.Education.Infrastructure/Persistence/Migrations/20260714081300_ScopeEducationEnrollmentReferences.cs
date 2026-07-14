using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScopeEducationEnrollmentReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_education_enrollments_education_institutions_institution_id",
                table: "education_enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_education_enrollments_education_student_profiles_resident_id",
                table: "education_enrollments");

            migrationBuilder.DropIndex(
                name: "IX_education_enrollments_resident_id",
                table: "education_enrollments");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_education_student_profiles_simulation_host_id_resident_id",
                table: "education_student_profiles",
                columns: new[] { "simulation_host_id", "resident_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_education_institutions_simulation_host_id_institution_id",
                table: "education_institutions",
                columns: new[] { "simulation_host_id", "institution_id" });

            migrationBuilder.CreateIndex(
                name: "IX_education_enrollments_simulation_host_id_institution_id",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "institution_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_education_enrollments_education_institutions_simulation_hos~",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "institution_id" },
                principalTable: "education_institutions",
                principalColumns: new[] { "simulation_host_id", "institution_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_education_enrollments_education_student_profiles_simulation~",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "resident_id" },
                principalTable: "education_student_profiles",
                principalColumns: new[] { "simulation_host_id", "resident_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_education_enrollments_education_institutions_simulation_hos~",
                table: "education_enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_education_enrollments_education_student_profiles_simulation~",
                table: "education_enrollments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_education_student_profiles_simulation_host_id_resident_id",
                table: "education_student_profiles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_education_institutions_simulation_host_id_institution_id",
                table: "education_institutions");

            migrationBuilder.DropIndex(
                name: "IX_education_enrollments_simulation_host_id_institution_id",
                table: "education_enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_education_enrollments_resident_id",
                table: "education_enrollments",
                column: "resident_id");

            migrationBuilder.AddForeignKey(
                name: "FK_education_enrollments_education_institutions_institution_id",
                table: "education_enrollments",
                column: "institution_id",
                principalTable: "education_institutions",
                principalColumn: "institution_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_education_enrollments_education_student_profiles_resident_id",
                table: "education_enrollments",
                column: "resident_id",
                principalTable: "education_student_profiles",
                principalColumn: "resident_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
