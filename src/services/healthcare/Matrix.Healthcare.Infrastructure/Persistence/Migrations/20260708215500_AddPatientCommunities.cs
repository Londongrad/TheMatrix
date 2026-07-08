using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientCommunities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "community_id",
                table: "healthcare_patient_medical_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_medical_records_host_community",
                table: "healthcare_patient_medical_records",
                columns: new[] { "simulation_host_id", "community_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_healthcare_medical_records_host_community",
                table: "healthcare_patient_medical_records");

            migrationBuilder.DropColumn(
                name: "community_id",
                table: "healthcare_patient_medical_records");
        }
    }
}
