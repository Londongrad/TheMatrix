using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopologyOrderedReadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_CityId_Type_Name",
                table: "RoadSegments",
                columns: new[] { "CityId", "Type", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_RoadNodes_CityId_Type_Name",
                table: "RoadNodes",
                columns: new[] { "CityId", "Type", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentialBuildings_CityId_Name",
                table: "ResidentialBuildings",
                columns: new[] { "CityId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Districts_CityId_Name",
                table: "Districts",
                columns: new[] { "CityId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_CityAnchors_CityId_Type_Name",
                table: "CityAnchors",
                columns: new[] { "CityId", "Type", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoadSegments_CityId_Type_Name",
                table: "RoadSegments");

            migrationBuilder.DropIndex(
                name: "IX_RoadNodes_CityId_Type_Name",
                table: "RoadNodes");

            migrationBuilder.DropIndex(
                name: "IX_ResidentialBuildings_CityId_Name",
                table: "ResidentialBuildings");

            migrationBuilder.DropIndex(
                name: "IX_Districts_CityId_Name",
                table: "Districts");

            migrationBuilder.DropIndex(
                name: "IX_CityAnchors_CityId_Type_Name",
                table: "CityAnchors");
        }
    }
}
