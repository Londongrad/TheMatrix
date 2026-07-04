using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientCareAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "healthcare_patient_care_assignments",
                columns: table => new
                {
                    patient_care_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    care_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    care_date = table.Column<DateOnly>(type: "date", nullable: false),
                    urgency = table.Column<int>(type: "integer", nullable: false),
                    assessment_revision = table.Column<long>(type: "bigint", nullable: false),
                    lifecycle_revision = table.Column<long>(type: "bigint", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_patient_care_assignments", x => x.patient_care_assignment_id);
                    table.CheckConstraint("ck_healthcare_patient_care_assignments_assessment_revision", "assessment_revision >= 0");
                    table.CheckConstraint("ck_healthcare_patient_care_assignments_lifecycle_revision", "lifecycle_revision >= 0");
                    table.ForeignKey(
                        name: "FK_healthcare_patient_care_assignments_healthcare_care_facilit~",
                        column: x => x.care_facility_id,
                        principalTable: "healthcare_care_facilities",
                        principalColumn: "care_facility_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_patient_care_assignments_capacity_usage",
                table: "healthcare_patient_care_assignments",
                columns: new[] { "simulation_host_id", "care_date", "care_facility_id" });

            migrationBuilder.CreateIndex(
                name: "IX_healthcare_patient_care_assignments_care_facility_id",
                table: "healthcare_patient_care_assignments",
                column: "care_facility_id");

            migrationBuilder.CreateIndex(
                name: "ux_healthcare_patient_care_assignments_patient_date",
                table: "healthcare_patient_care_assignments",
                columns: new[] { "simulation_host_id", "patient_id", "care_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "healthcare_patient_care_assignments");
        }
    }
}
