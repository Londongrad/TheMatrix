using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHouseholdAccountExternalReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_City_Household_Account_city_id_external_reference_code",
                table: "City_Household_Account",
                columns: new[] { "city_id", "external_reference_code" },
                unique: true,
                filter: "\"external_reference_code\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Household_Account_city_id_external_reference_code",
                table: "City_Household_Account");
        }
    }
}
