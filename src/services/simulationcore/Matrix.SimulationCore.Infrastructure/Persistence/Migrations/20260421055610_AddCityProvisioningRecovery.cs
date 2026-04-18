using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.SimulationCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCityProvisioningRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProvisioningAttemptCount",
                table: "Cities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProvisioningHeartbeatAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProvisioningLeaseExpiresAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProvisioningStartedAtUtc",
                table: "Cities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                sql:
                """
                UPDATE "Cities"
                SET "ProvisioningStartedAtUtc" = COALESCE("ProvisioningStartedAtUtc", "CreatedAtUtc"),
                    "ProvisioningHeartbeatAtUtc" = COALESCE("ProvisioningHeartbeatAtUtc", "CreatedAtUtc")
                WHERE "Status" = 3;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_ProvisioningLeaseExpiresAtUtc",
                table: "Cities",
                column: "ProvisioningLeaseExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cities_ProvisioningLeaseExpiresAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ProvisioningAttemptCount",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ProvisioningHeartbeatAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ProvisioningLeaseExpiresAtUtc",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ProvisioningStartedAtUtc",
                table: "Cities");
        }
    }
}
