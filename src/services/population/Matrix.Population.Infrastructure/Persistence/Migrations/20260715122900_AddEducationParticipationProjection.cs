using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Population.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationParticipationProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EducationParticipationProjections",
                columns: table => new
                {
                    SimulationHostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipationRevision = table.Column<long>(type: "bigint", nullable: false),
                    ResidentLifecycleRevision = table.Column<long>(type: "bigint", nullable: false),
                    IsEnrolled = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveStage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InstitutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstitutionAnchorId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnrolledOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CompletedStage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CompletedStageOn = table.Column<DateOnly>(type: "date", nullable: true),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationParticipationProjections", x => new { x.SimulationHostId, x.ResidentId });
                    table.ForeignKey(
                        name: "FK_EducationParticipationProjections_Persons_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationParticipationProjections_ResidentId",
                table: "EducationParticipationProjections",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationParticipationProjections_SimulationHostId_Institut~",
                table: "EducationParticipationProjections",
                columns: new[] { "SimulationHostId", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationParticipationProjections_SimulationHostId_IsEnroll~",
                table: "EducationParticipationProjections",
                columns: new[] { "SimulationHostId", "IsEnrolled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationParticipationProjections");
        }
    }
}
