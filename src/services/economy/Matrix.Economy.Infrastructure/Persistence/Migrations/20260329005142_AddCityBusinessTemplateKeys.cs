using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Economy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityBusinessTemplateKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "template_key",
                table: "City_Business",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_City_Business_city_id_template_key",
                table: "City_Business",
                columns: new[] { "city_id", "template_key" },
                unique: true,
                filter: "\"template_key\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_City_Business_city_id_template_key",
                table: "City_Business");

            migrationBuilder.DropColumn(
                name: "template_key",
                table: "City_Business");
        }
    }
}
