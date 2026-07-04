using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackPatientCareAssignmentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cancellation_reason",
                table: "healthcare_patient_care_assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at_utc",
                table: "healthcare_patient_care_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "closed_on",
                table: "healthcare_patient_care_assignments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "healthcare_patient_care_assignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "treatment_health_delta",
                table: "healthcare_patient_care_assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "treatment_medical_state_changed",
                table: "healthcare_patient_care_assignments",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_patient_care_assignments_due_lookup",
                table: "healthcare_patient_care_assignments",
                columns: new[] { "simulation_host_id", "status", "patient_id", "care_date" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_healthcare_patient_care_assignments_cancellation_reason",
                table: "healthcare_patient_care_assignments",
                sql: "cancellation_reason IS NULL OR cancellation_reason BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "ck_healthcare_patient_care_assignments_closure",
                table: "healthcare_patient_care_assignments",
                sql: "(status = 0 AND closed_on IS NULL AND closed_at_utc IS NULL AND cancellation_reason IS NULL AND treatment_health_delta IS NULL AND treatment_medical_state_changed IS NULL) OR (status = 1 AND closed_on IS NOT NULL AND closed_at_utc IS NOT NULL AND cancellation_reason IS NULL AND treatment_health_delta IS NOT NULL AND treatment_medical_state_changed IS NOT NULL) OR (status = 2 AND closed_on IS NOT NULL AND closed_at_utc IS NOT NULL AND cancellation_reason IS NOT NULL AND treatment_health_delta IS NULL AND treatment_medical_state_changed IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_healthcare_patient_care_assignments_status",
                table: "healthcare_patient_care_assignments",
                sql: "status BETWEEN 0 AND 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_healthcare_patient_care_assignments_due_lookup",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_healthcare_patient_care_assignments_cancellation_reason",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_healthcare_patient_care_assignments_closure",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_healthcare_patient_care_assignments_status",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropColumn(
                name: "closed_at_utc",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropColumn(
                name: "closed_on",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropColumn(
                name: "treatment_health_delta",
                table: "healthcare_patient_care_assignments");

            migrationBuilder.DropColumn(
                name: "treatment_medical_state_changed",
                table: "healthcare_patient_care_assignments");
        }
    }
}
