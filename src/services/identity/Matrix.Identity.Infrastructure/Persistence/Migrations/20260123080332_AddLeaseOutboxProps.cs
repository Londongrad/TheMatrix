using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseOutboxProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                table: "OutboxMessages");

            migrationBuilder.AlterColumn<string>(
                name: "Error",
                table: "OutboxMessages",
                type: "text",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptOnUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockToken",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptOnUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc_NextAttemptOnU~",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "LockedUntilUtc", "NextAttemptOnUtc", "OccurredOnUtc" },
                filter: "\"ProcessedOnUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc_LockedUntilUtc_NextAttemptOnU~",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LastAttemptOnUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockToken",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextAttemptOnUtc",
                table: "OutboxMessages");

            migrationBuilder.AlterColumn<string>(
                name: "Error",
                table: "OutboxMessages",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOnUtc",
                table: "OutboxMessages",
                column: "ProcessedOnUtc");
        }
    }
}
