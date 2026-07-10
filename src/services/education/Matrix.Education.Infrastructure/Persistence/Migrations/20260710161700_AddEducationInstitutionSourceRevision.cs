using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationInstitutionSourceRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_source_revision",
                table: "education_institutions",
                type: "bigint",
                nullable: false,
                defaultValue: -1L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_synchronized_at_utc",
                table: "education_institutions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_source_revision",
                table: "education_institutions");

            migrationBuilder.DropColumn(
                name: "last_synchronized_at_utc",
                table: "education_institutions");
        }
    }
}
