using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationAttendanceObservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AttendanceIndex",
                table: "EducationParticipationProjections",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AttendanceObservedAtSimTimeUtc",
                table: "EducationParticipationProjections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AttendanceSourceTickId",
                table: "EducationParticipationProjections",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommuteAccessibilityIndex",
                table: "EducationParticipationProjections",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceIndex",
                table: "EducationParticipationProjections");

            migrationBuilder.DropColumn(
                name: "AttendanceObservedAtSimTimeUtc",
                table: "EducationParticipationProjections");

            migrationBuilder.DropColumn(
                name: "AttendanceSourceTickId",
                table: "EducationParticipationProjections");

            migrationBuilder.DropColumn(
                name: "CommuteAccessibilityIndex",
                table: "EducationParticipationProjections");
        }
    }
}
