using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(HealthcareDbContext))]
    [Migration("20260701110200_AddMedicalRecordLifecycleRevision")]
    public sealed class AddMedicalRecordLifecycleRevision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_lifecycle_revision",
                table: "healthcare_patient_medical_records",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_lifecycle_revision",
                table: "healthcare_patient_medical_records");
        }
    }
}
