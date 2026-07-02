using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations;

[DbContext(typeof(HealthcareDbContext))]
[Migration("20260702213700_AddPatientCareNeeds")]
public sealed class AddPatientCareNeeds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "healthcare_patient_care_needs",
            columns: table => new
            {
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                urgency = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                requested_on = table.Column<DateTime>(type: "date", nullable: false),
                resolved_on = table.Column<DateTime>(type: "date", nullable: true),
                last_assessment_revision = table.Column<long>(type: "bigint", nullable: false),
                last_lifecycle_revision = table.Column<long>(type: "bigint", nullable: false),
                last_assessed_at_utc = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_healthcare_patient_care_needs", x => x.patient_id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_healthcare_patient_care_needs_allocation_candidates",
            table: "healthcare_patient_care_needs",
            columns: new[] { "simulation_host_id", "is_active", "urgency", "requested_on" },
            descending: new[] { false, false, true, false });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "healthcare_patient_care_needs");
    }
}
