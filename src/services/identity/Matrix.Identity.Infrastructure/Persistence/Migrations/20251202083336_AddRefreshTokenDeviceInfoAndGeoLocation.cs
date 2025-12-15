using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenDeviceInfoAndGeoLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "UserRefreshTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "UserRefreshTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "UserRefreshTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "UserRefreshTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "UserRefreshTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "UserRefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAtUtc",
                table: "UserRefreshTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "UserRefreshTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "UserRefreshTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_TokenHash",
                table: "UserRefreshTokens",
                column: "TokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_TokenHash",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "City",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastUsedAtUtc",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "UserRefreshTokens");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "UserRefreshTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
