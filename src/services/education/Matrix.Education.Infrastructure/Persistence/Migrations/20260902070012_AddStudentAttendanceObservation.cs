using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAttendanceObservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "attendance_index",
                table: "education_student_profiles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "attendance_observed_at_sim_time_utc",
                table: "education_student_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "commute_accessibility_index",
                table: "education_student_profiles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "last_attendance_source_tick_id",
                table: "education_student_profiles",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attendance_index",
                table: "education_student_profiles");

            migrationBuilder.DropColumn(
                name: "attendance_observed_at_sim_time_utc",
                table: "education_student_profiles");

            migrationBuilder.DropColumn(
                name: "commute_accessibility_index",
                table: "education_student_profiles");

            migrationBuilder.DropColumn(
                name: "last_attendance_source_tick_id",
                table: "education_student_profiles");
        }
    }
}
