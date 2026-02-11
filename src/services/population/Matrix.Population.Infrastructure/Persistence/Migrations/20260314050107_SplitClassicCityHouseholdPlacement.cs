using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitClassicCityHouseholdPlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassicCityHouseholdPlacements",
                columns: table => new
                {
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResidentialBuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    HousingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassicCityHouseholdPlacements", x => x.HouseholdId);
                    table.ForeignKey(
                        name: "FK_ClassicCityHouseholdPlacements_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "ClassicCityHouseholdPlacements" ("HouseholdId", "CityId", "DistrictId", "ResidentialBuildingId", "HousingStatus")
                SELECT "Id", "CityId", "DistrictId", "ResidentialBuildingId", "HousingStatus"
                FROM "Households"
                WHERE "CityId" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ClassicCityHouseholdPlacements_CityId",
                table: "ClassicCityHouseholdPlacements",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassicCityHouseholdPlacements_DistrictId",
                table: "ClassicCityHouseholdPlacements",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassicCityHouseholdPlacements_ResidentialBuildingId",
                table: "ClassicCityHouseholdPlacements",
                column: "ResidentialBuildingId");

            migrationBuilder.DropIndex(
                name: "IX_Households_CityId",
                table: "Households");

            migrationBuilder.DropIndex(
                name: "IX_Households_DistrictId",
                table: "Households");

            migrationBuilder.DropIndex(
                name: "IX_Households_ResidentialBuildingId",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "HousingStatus",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "ResidentialBuildingId",
                table: "Households");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassicCityHouseholdPlacements");

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "Households",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "Households",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HousingStatus",
                table: "Households",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Homeless");

            migrationBuilder.AddColumn<Guid>(
                name: "ResidentialBuildingId",
                table: "Households",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Households" AS h
                SET
                    "CityId" = p."CityId",
                    "DistrictId" = p."DistrictId",
                    "ResidentialBuildingId" = p."ResidentialBuildingId",
                    "HousingStatus" = p."HousingStatus"
                FROM "ClassicCityHouseholdPlacements" AS p
                WHERE h."Id" = p."HouseholdId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Households_CityId",
                table: "Households",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_DistrictId",
                table: "Households",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_ResidentialBuildingId",
                table: "Households",
                column: "ResidentialBuildingId");
        }
    }
}
