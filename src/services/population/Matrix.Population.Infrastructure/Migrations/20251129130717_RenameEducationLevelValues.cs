using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameEducationLevelValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Persons"
                SET "EducationLevel" = 'LowerSecondary'
                WHERE "EducationLevel" = 'Secondary';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Persons"
                SET "EducationLevel" = 'Secondary'
                WHERE "EducationLevel" = 'LowerSecondary';
            """);
        }
    }
}
