using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "UserRefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Country = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Region = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshTokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPersistent = table.Column<bool>(type: "boolean", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "UserSessions" (
                    "Id",
                    "UserId",
                    "DeviceId",
                    "DeviceName",
                    "UserAgent",
                    "IpAddress",
                    "Country",
                    "Region",
                    "City",
                    "CreatedAtUtc",
                    "LastUsedAtUtc",
                    "RefreshTokenExpiresAtUtc",
                    "IsPersistent",
                    "IsRevoked",
                    "RevokedAtUtc",
                    "RevokedReason"
                )
                SELECT
                    t."Id",
                    t."UserId",
                    t."DeviceId",
                    t."DeviceName",
                    t."UserAgent",
                    t."IpAddress",
                    t."Country",
                    t."Region",
                    t."City",
                    t."CreatedAtUtc",
                    t."LastUsedAtUtc",
                    t."ExpiresAtUtc",
                    t."IsPersistent",
                    t."IsRevoked",
                    t."RevokedAtUtc",
                    t."RevokedReason"
                FROM "UserRefreshTokens" t;

                UPDATE "UserRefreshTokens"
                SET "SessionId" = "Id"
                WHERE "SessionId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SessionId",
                table: "UserRefreshTokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_SessionId",
                table: "UserRefreshTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_DeviceId",
                table: "UserSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsRevoked",
                table: "UserSessions",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_RefreshTokenExpiresAtUtc",
                table: "UserSessions",
                columns: new[] { "UserId", "RefreshTokenExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_SessionId",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "UserRefreshTokens");
        }
    }
}
