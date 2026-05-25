using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityRoadTopology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccessRoadNodeId",
                table: "ResidentialBuildings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "PositionX",
                table: "ResidentialBuildings",
                type: "numeric(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PositionY",
                table: "ResidentialBuildings",
                type: "numeric(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AnchorX",
                table: "Districts",
                type: "numeric(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AnchorY",
                table: "Districts",
                type: "numeric(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RoadNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PositionX = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    PositionY = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadNodes_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadNodes_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    LengthMeters = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadSegments_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadSegments_Districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoadSegments_RoadNodes_FromRoadNodeId",
                        column: x => x.FromRoadNodeId,
                        principalTable: "RoadNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadSegments_RoadNodes_ToRoadNodeId",
                        column: x => x.ToRoadNodeId,
                        principalTable: "RoadNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResidentialBuildings_AccessRoadNodeId",
                table: "ResidentialBuildings",
                column: "AccessRoadNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadNodes_CityId",
                table: "RoadNodes",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadNodes_DistrictId",
                table: "RoadNodes",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_CityId",
                table: "RoadSegments",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_DistrictId",
                table: "RoadSegments",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_FromRoadNodeId",
                table: "RoadSegments",
                column: "FromRoadNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_ToRoadNodeId",
                table: "RoadSegments",
                column: "ToRoadNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResidentialBuildings_RoadNodes_AccessRoadNodeId",
                table: "ResidentialBuildings",
                column: "AccessRoadNodeId",
                principalTable: "RoadNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResidentialBuildings_RoadNodes_AccessRoadNodeId",
                table: "ResidentialBuildings");

            migrationBuilder.DropTable(
                name: "RoadSegments");

            migrationBuilder.DropTable(
                name: "RoadNodes");

            migrationBuilder.DropIndex(
                name: "IX_ResidentialBuildings_AccessRoadNodeId",
                table: "ResidentialBuildings");

            migrationBuilder.DropColumn(
                name: "AccessRoadNodeId",
                table: "ResidentialBuildings");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "ResidentialBuildings");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "ResidentialBuildings");

            migrationBuilder.DropColumn(
                name: "AnchorX",
                table: "Districts");

            migrationBuilder.DropColumn(
                name: "AnchorY",
                table: "Districts");
        }
    }
}
