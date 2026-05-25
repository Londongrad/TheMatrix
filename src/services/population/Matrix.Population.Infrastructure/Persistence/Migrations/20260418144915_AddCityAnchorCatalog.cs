using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityAnchorCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityPopulationAnchorCatalogItems",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityAnchorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    PositionX = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PositionY = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityPopulationAnchorCatalogItems", x => new { x.CityId, x.CityAnchorId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationAnchorCatalogItems_CityId_Type",
                table: "CityPopulationAnchorCatalogItems",
                columns: new[] { "CityId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CityPopulationAnchorCatalogItems_DistrictId",
                table: "CityPopulationAnchorCatalogItems",
                column: "DistrictId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityPopulationAnchorCatalogItems");
        }
    }
}
