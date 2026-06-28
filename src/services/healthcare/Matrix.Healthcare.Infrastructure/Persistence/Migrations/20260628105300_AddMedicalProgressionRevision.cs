using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalProgressionRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_progression_revision",
                table: "healthcare_patient_medical_records",
                type: "bigint",
                nullable: false,
                defaultValue: -1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_progression_revision",
                table: "healthcare_patient_medical_records");
        }
    }
}
