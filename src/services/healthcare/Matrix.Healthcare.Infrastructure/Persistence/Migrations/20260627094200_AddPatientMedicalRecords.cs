using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientMedicalRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "healthcare_patient_medical_records",
                columns: table => new
                {
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    health_score = table.Column<int>(type: "integer", nullable: false),
                    illness_kind = table.Column<int>(type: "integer", nullable: true),
                    illness_severity = table.Column<int>(type: "integer", nullable: true),
                    illness_diagnosed_on = table.Column<DateOnly>(type: "date", nullable: true),
                    last_illness_recovered_on = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_patient_medical_records", x => x.patient_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_medical_records_simulation_host",
                table: "healthcare_patient_medical_records",
                column: "simulation_host_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "healthcare_patient_medical_records");
        }
    }
}
