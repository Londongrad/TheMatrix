using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityEnvironmentalConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DrainageKind = table.Column<int>(type: "integer", nullable: false),
                    DrainageLoadIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    DrainageServiceQualityIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    DrainageBacklogIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    DrainageFailureRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SnowRemovalKind = table.Column<int>(type: "integer", nullable: false),
                    SnowRemovalLoadIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SnowRemovalServiceQualityIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SnowRemovalBacklogIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SnowRemovalFailureRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RoadAccessKind = table.Column<int>(type: "integer", nullable: false),
                    RoadAccessLoadIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RoadAccessServiceQualityIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RoadAccessBacklogIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RoadAccessFailureRiskIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    FloodingIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    SnowAccumulationIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RoadAccessibilityIndex = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityEnvironmentalConditions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityEnvironmentalConditions");
        }
    }
}
