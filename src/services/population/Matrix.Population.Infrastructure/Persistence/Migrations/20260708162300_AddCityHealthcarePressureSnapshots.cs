using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityHealthcarePressureSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityHealthcarePressureSnapshots",
                columns: table => new
                {
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceRevision = table.Column<long>(type: "bigint", nullable: false),
                    CurrentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PatientCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveIllnessCount = table.Column<int>(type: "integer", nullable: false),
                    SevereIllnessCount = table.Column<int>(type: "integer", nullable: false),
                    MedicalLoadIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    TriagePressureIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    RecoverySupportIndex = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityHealthcarePressureSnapshots", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityHealthcarePressureSnapshots_SourceRevision",
                table: "CityHealthcarePressureSnapshots",
                column: "SourceRevision");

            migrationBuilder.CreateIndex(
                name: "IX_CityHealthcarePressureSnapshots_UpdatedAtUtc",
                table: "CityHealthcarePressureSnapshots",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityHealthcarePressureSnapshots");
        }
    }
}
