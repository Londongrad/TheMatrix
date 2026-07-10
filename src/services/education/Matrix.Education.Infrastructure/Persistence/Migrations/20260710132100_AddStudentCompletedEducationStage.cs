using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentCompletedEducationStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "completed_stage",
                table: "education_student_profiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_stage_on",
                table: "education_student_profiles",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed_stage",
                table: "education_student_profiles");

            migrationBuilder.DropColumn(
                name: "completed_stage_on",
                table: "education_student_profiles");
        }
    }
}
