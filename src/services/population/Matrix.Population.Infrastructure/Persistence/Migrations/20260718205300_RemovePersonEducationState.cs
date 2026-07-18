using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PopulationDbContext))]
    [Migration("20260718205300_RemovePersonEducationState")]
    public sealed class RemovePersonEducationState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationInstitutionAnchorId",
                table: "Persons");
            migrationBuilder.DropColumn(
                name: "EducationInstitutionId",
                table: "Persons");
            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "Persons");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EducationInstitutionAnchorId",
                table: "Persons",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "EducationInstitutionId",
                table: "Persons",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "Persons",
                type: "text",
                nullable: false,
                defaultValue: "None");
        }
    }
}
