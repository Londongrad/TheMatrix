using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityActiveTripOrderingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_CityId_Status_StartedAtSimTimeUtc_Id",
                table: "CityActiveTrips",
                columns: new[] { "CityId", "Status", "StartedAtSimTimeUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CityActiveTrips_CityId_Status_StartedAtSimTimeUtc_Id",
                table: "CityActiveTrips");
        }
    }
}
