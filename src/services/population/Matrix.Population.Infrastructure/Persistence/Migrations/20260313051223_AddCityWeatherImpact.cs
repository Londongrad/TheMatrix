using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityWeatherImpact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationWeatherImpactStates",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAppliedAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAppliedOccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationWeatherImpactStates", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedIntegrationMessages",
                columns: table => new
                {
                    Consumer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedIntegrationMessages", x => new { x.Consumer, x.MessageId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationWeatherImpactStates_UpdatedAtUtc",
                table: "CityPopulationWeatherImpactStates",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedIntegrationMessages_ProcessedAtUtc",
                table: "ProcessedIntegrationMessages",
                column: "ProcessedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationWeatherImpactStates");

            migrationBuilder.DropTable(
                name: "ProcessedIntegrationMessages");
        }
    }
}
