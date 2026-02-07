using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonNeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Energy",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 70);

            migrationBuilder.AddColumn<int>(
                name: "SocialNeed",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 35);

            migrationBuilder.AddColumn<int>(
                name: "Stress",
                table: "Persons",
                type: "integer",
                nullable: false,
                defaultValue: 25);

            migrationBuilder.Sql(
                sql: """
                     UPDATE "Persons"
                     SET "Energy" = CASE
                         WHEN "EmploymentStatus" = 'Employed' THEN 65
                         WHEN "EmploymentStatus" = 'Student' THEN 68
                         WHEN "EmploymentStatus" = 'Retired' THEN 72
                         ELSE 70
                     END,
                         "Stress" = CASE
                         WHEN "EmploymentStatus" = 'Employed' THEN 42
                         WHEN "EmploymentStatus" = 'Student' THEN 38
                         WHEN "EmploymentStatus" = 'Unemployed' THEN 34
                         WHEN "EmploymentStatus" = 'Retired' THEN 18
                         ELSE 25
                     END,
                         "SocialNeed" = CASE
                         WHEN "MaritalStatus" = 'Married' THEN 28
                         WHEN "MaritalStatus" = 'Widowed' THEN 55
                         WHEN "MaritalStatus" = 'Divorced' THEN 50
                         ELSE 42
                     END;
                     """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Energy",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "SocialNeed",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "Stress",
                table: "Persons");
        }
    }
}
