using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareOperationalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "healthcare_care_medicine_supply_states",
                columns: table => new
                {
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_level_index = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    shortage_risk_index = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    last_source_revision = table.Column<long>(type: "bigint", nullable: false),
                    last_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_care_medicine_supply_states", x => x.simulation_host_id);
                    table.CheckConstraint("ck_healthcare_care_medicine_shortage_risk", "shortage_risk_index BETWEEN 0 AND 1");
                    table.CheckConstraint("ck_healthcare_care_medicine_source_revision", "last_source_revision >= 0");
                    table.CheckConstraint("ck_healthcare_care_medicine_stock_level", "stock_level_index BETWEEN 0 AND 1");
                });

            migrationBuilder.CreateTable(
                name: "healthcare_care_service_quality_states",
                columns: table => new
                {
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quality_multiplier = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    last_observed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_care_service_quality_states", x => x.simulation_host_id);
                    table.CheckConstraint("ck_healthcare_care_service_quality_multiplier", "quality_multiplier BETWEEN 0 AND 2");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "healthcare_care_medicine_supply_states");

            migrationBuilder.DropTable(
                name: "healthcare_care_service_quality_states");
        }
    }
}
