using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matrix.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultUserAccessPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefaultUserAccessPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultUserAccessPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaultUserAccessOverrides",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Effect = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultUserAccessOverrides", x => new { x.PolicyId, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_DefaultUserAccessOverrides_DefaultUserAccessPolicies_Policy~",
                        column: x => x.PolicyId,
                        principalTable: "DefaultUserAccessPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DefaultUserAccessOverrides_Permissions_PermissionKey",
                        column: x => x.PermissionKey,
                        principalTable: "Permissions",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DefaultUserAccessOverrides_Effect",
                table: "DefaultUserAccessOverrides",
                column: "Effect");

            migrationBuilder.CreateIndex(
                name: "IX_DefaultUserAccessOverrides_PermissionKey",
                table: "DefaultUserAccessOverrides",
                column: "PermissionKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DefaultUserAccessOverrides");

            migrationBuilder.DropTable(
                name: "DefaultUserAccessPolicies");
        }
    }
}
