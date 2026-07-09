using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHealthcareDistrictHealthSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityHealthcareDistrictHealthSnapshots",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveIllnessCount = table.Column<int>(type: "integer", nullable: false),
                    SevereIllnessCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityHealthcareDistrictHealthSnapshots", x => new { x.CityId, x.DistrictId });
                    table.ForeignKey(
                        name: "FK_CityHealthcareDistrictHealthSnapshots_CityHealthcarePressur~",
                        column: x => x.CityId,
                        principalTable: "CityHealthcarePressureSnapshots",
                        principalColumn: "CityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityHealthcareDistrictHealthSnapshots_DistrictId",
                table: "CityHealthcareDistrictHealthSnapshots",
                column: "DistrictId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityHealthcareDistrictHealthSnapshots");
        }
    }
}
