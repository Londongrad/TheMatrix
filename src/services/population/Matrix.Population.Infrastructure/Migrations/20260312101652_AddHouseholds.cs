using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: true),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResidentialBuildingId = table.Column<Guid>(type: "uuid", nullable: true),
                    HousingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_HouseholdId",
                table: "Persons",
                column: "HouseholdId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Persons_Households_HouseholdId",
                table: "Persons",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_Households_HouseholdId",
                table: "Persons");

            migrationBuilder.DropTable(
                name: "Households");

            migrationBuilder.DropIndex(
                name: "IX_Persons_HouseholdId",
                table: "Persons");
        }
    }
}
