using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityPopulationServiceQualityState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationServiceQualityStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    HealthcareQualityIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    EducationQualityIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    HousingSupportIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationServiceQualityStates", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationServiceQualityStates_UpdatedAtUtc",
                table: "CityPopulationServiceQualityStates",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationServiceQualityStates");
        }
    }
}
