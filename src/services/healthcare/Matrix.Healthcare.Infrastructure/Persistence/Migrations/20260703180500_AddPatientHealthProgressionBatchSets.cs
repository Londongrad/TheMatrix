using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Healthcare.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientHealthProgressionBatchSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "healthcare_patient_health_progression_batch_sets",
                columns: table => new
                {
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_revision = table.Column<long>(type: "bigint", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    total_batches = table.Column<int>(type: "integer", nullable: false),
                    received_batch_count = table.Column<int>(type: "integer", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    first_received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_batch_map = table.Column<byte[]>(type: "bytea", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_healthcare_patient_health_progression_batch_sets", x => new { x.simulation_host_id, x.source_revision });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "healthcare_patient_health_progression_batch_sets");
        }
    }
}
