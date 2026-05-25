using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityBusinessExternalReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_reference_code",
                table: "City_Business",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_city_id_external_reference_code",
                table: "City_Business",
                columns: new[] { "city_id", "external_reference_code" },
                unique: true,
                filter: "\"external_reference_code\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Business_city_id_external_reference_code",
                table: "City_Business");

            migrationBuilder.DropColumn(
                name: "external_reference_code",
                table: "City_Business");
        }
    }
}
