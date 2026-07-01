using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(HealthcareDbContext))]
    [Migration("20260701173700_AddCareFacilityCatalog")]
    public sealed class AddCareFacilityCatalog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "healthcare_care_facilities",
                columns: table => new
                {
                    care_facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    location_anchor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    daily_patient_capacity = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_source_revision = table.Column<long>(type: "bigint", nullable: false),
                    last_synchronized_at_utc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_care_facilities", x => x.care_facility_id);
                    table.CheckConstraint(
                        "ck_healthcare_care_facilities_daily_capacity_positive",
                        "daily_patient_capacity > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_healthcare_care_facilities_capacity_candidates",
                table: "healthcare_care_facilities",
                columns: new[] { "simulation_host_id", "is_active", "kind" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "healthcare_care_facilities");
        }
    }
}
