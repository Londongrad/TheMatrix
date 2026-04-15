using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityActiveTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CityActiveTrips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravellerEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    Profile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MovementCapabilityIndex = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    UsedDynamicRoadConditions = table.Column<bool>(type: "boolean", nullable: false),
                    PlannedAtTickId = table.Column<long>(type: "bigint", nullable: false),
                    ConditionsEffectiveTickId = table.Column<long>(type: "bigint", nullable: true),
                    StartedAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAdvancedAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpectedArrivalAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArrivedAtSimTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAdvancedTickId = table.Column<long>(type: "bigint", nullable: false),
                    TotalDistanceMeters = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PlannedTravelTimeMinutes = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    AdjustedTravelTimeMinutes = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    ProgressIndex = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    DistanceTravelledMeters = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    FromKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FromEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromDistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FromPositionX = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    FromPositionY = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ToKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ToEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToDistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ToPositionX = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ToPositionY = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CurrentDistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentRoadSegmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentSegmentProgressIndex = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    CurrentPositionX = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    CurrentPositionY = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityActiveTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_Districts_CurrentDistrictId",
                        column: x => x.CurrentDistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_Districts_FromDistrictId",
                        column: x => x.FromDistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_Districts_ToDistrictId",
                        column: x => x.ToDistrictId,
                        principalTable: "Districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_RoadNodes_FromRoadNodeId",
                        column: x => x.FromRoadNodeId,
                        principalTable: "RoadNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_RoadNodes_ToRoadNodeId",
                        column: x => x.ToRoadNodeId,
                        principalTable: "RoadNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CityActiveTrips_RoadSegments_CurrentRoadSegmentId",
                        column: x => x.CurrentRoadSegmentId,
                        principalTable: "RoadSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CityActiveTripSegments",
                columns: table => new
                {
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CityActiveTripId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToRoadNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LengthMeters = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    EstimatedTraversalMinutes = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    FromPositionX = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    FromPositionY = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ToPositionX = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ToPositionY = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityActiveTripSegments", x => new { x.CityActiveTripId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_CityActiveTripSegments_CityActiveTrips_CityActiveTripId",
                        column: x => x.CityActiveTripId,
                        principalTable: "CityActiveTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_CityId",
                table: "CityActiveTrips",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_CityId_Status",
                table: "CityActiveTrips",
                columns: new[] { "CityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_CurrentDistrictId",
                table: "CityActiveTrips",
                column: "CurrentDistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_CurrentRoadSegmentId",
                table: "CityActiveTrips",
                column: "CurrentRoadSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_FromDistrictId",
                table: "CityActiveTrips",
                column: "FromDistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_FromRoadNodeId",
                table: "CityActiveTrips",
                column: "FromRoadNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_Status",
                table: "CityActiveTrips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_ToDistrictId",
                table: "CityActiveTrips",
                column: "ToDistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_ToRoadNodeId",
                table: "CityActiveTrips",
                column: "ToRoadNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTrips_TravellerEntityId",
                table: "CityActiveTrips",
                column: "TravellerEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTripSegments_CityActiveTripId",
                table: "CityActiveTripSegments",
                column: "CityActiveTripId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTripSegments_DistrictId",
                table: "CityActiveTripSegments",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_CityActiveTripSegments_RoadSegmentId",
                table: "CityActiveTripSegments",
                column: "RoadSegmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CityActiveTripSegments");

            migrationBuilder.DropTable(
                name: "CityActiveTrips");
        }
    }
}
