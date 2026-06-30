using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(HealthcareDbContext))]
    [Migration("20260630211300_AddPatientProfileLifecycleRevision")]
    public sealed class AddPatientProfileLifecycleRevision : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_lifecycle_revision",
                table: "healthcare_patient_profiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_lifecycle_revision",
                table: "healthcare_patient_profiles");
        }
    }
}
