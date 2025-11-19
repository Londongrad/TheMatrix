using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    MiddleName = table.Column<string>(type: "text", nullable: true),
                    LifeStatus = table.Column<string>(type: "text", nullable: false),
                    Sex = table.Column<string>(type: "text", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "date", nullable: false),
                    MaritalStatus = table.Column<string>(type: "text", nullable: false),
                    EducationLevel = table.Column<string>(type: "text", nullable: false),
                    Health = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Happiness = table.Column<int>(type: "integer", nullable: false),
                    EmploymentStatus = table.Column<string>(type: "text", nullable: false),
                    WorkplaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Optimism = table.Column<int>(type: "integer", nullable: false),
                    Discipline = table.Column<int>(type: "integer", nullable: false),
                    RiskTolerance = table.Column<int>(type: "integer", nullable: false),
                    Sociability = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Persons");
        }
    }
}
