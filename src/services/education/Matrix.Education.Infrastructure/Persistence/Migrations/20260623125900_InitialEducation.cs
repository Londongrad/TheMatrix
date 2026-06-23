using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Education.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEducation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "education_institutions",
                columns: table => new
                {
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    location_anchor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    current_enrollment_count = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_institutions", x => x.institution_id);
                    table.CheckConstraint("ck_education_institutions_capacity_positive", "capacity > 0");
                    table.CheckConstraint("ck_education_institutions_enrollment_within_capacity", "current_enrollment_count >= 0 AND current_enrollment_count <= capacity");
                });

            migrationBuilder.CreateTable(
                name: "education_progression_checkpoints",
                columns: table => new
                {
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_completed_tick_id = table.Column<long>(type: "bigint", nullable: false),
                    last_completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_progression_checkpoints", x => x.simulation_host_id);
                    table.CheckConstraint("ck_education_progression_checkpoint_tick", "last_completed_tick_id >= 0");
                });

            migrationBuilder.CreateTable(
                name: "education_student_profiles",
                columns: table => new
                {
                    resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    birth_date = table.Column<DateTime>(type: "date", nullable: false),
                    is_alive = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_source_revision = table.Column<long>(type: "bigint", nullable: false),
                    last_synchronized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_student_profiles", x => x.resident_id);
                });

            migrationBuilder.CreateTable(
                name: "education_enrollments",
                columns: table => new
                {
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulation_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    institution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    enrolled_on = table.Column<DateTime>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    closed_on = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_enrollments", x => x.enrollment_id);
                    table.CheckConstraint("ck_education_enrollments_terminal_date", "(status = 'Active' AND closed_on IS NULL) OR (status <> 'Active' AND closed_on IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_education_enrollments_education_institutions_institution_id",
                        column: x => x.institution_id,
                        principalTable: "education_institutions",
                        principalColumn: "institution_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_education_enrollments_education_student_profiles_resident_id",
                        column: x => x.resident_id,
                        principalTable: "education_student_profiles",
                        principalColumn: "resident_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_education_enrollments_institution_status",
                table: "education_enrollments",
                columns: new[] { "institution_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_education_enrollments_resident_id",
                table: "education_enrollments",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "ix_education_enrollments_tick_candidates",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "status", "stage", "resident_id" });

            migrationBuilder.CreateIndex(
                name: "ux_education_enrollments_active_stage",
                table: "education_enrollments",
                columns: new[] { "simulation_host_id", "resident_id", "stage" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_education_institutions_capacity_candidates",
                table: "education_institutions",
                columns: new[] { "simulation_host_id", "is_active", "kind" });

            migrationBuilder.CreateIndex(
                name: "ix_education_profiles_tick_candidates",
                table: "education_student_profiles",
                columns: new[] { "simulation_host_id", "is_active", "is_alive", "birth_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "education_enrollments");

            migrationBuilder.DropTable(
                name: "education_progression_checkpoints");

            migrationBuilder.DropTable(
                name: "education_institutions");

            migrationBuilder.DropTable(
                name: "education_student_profiles");
        }
    }
}
