using System;
using Matrix.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Identity.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260325204618_AddUserEmailConfirmationTimeline")]
    public partial class AddUserEmailConfirmationTimeline : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "EmailConfirmedAtUtc" = "CreatedAtUtc"
                WHERE "IsEmailConfirmed" = TRUE
                  AND "EmailConfirmedAtUtc" IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmedAtUtc",
                table: "Users");
        }
    }
}
