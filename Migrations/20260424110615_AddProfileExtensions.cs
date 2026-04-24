using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UAM.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarFileId",
                table: "Users",
                type: "character varying(26)",
                maxLength: 26,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverFileId",
                table: "Users",
                type: "character varying(26)",
                maxLength: 26,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Handle",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LinksJson",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PronounsJson",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBadgeKind",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Users",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserPrivacySettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    UserProfileId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ProfileVisibility = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WhoCanMessage = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    WhoCanMention = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AllowIndexing = table.Column<bool>(type: "boolean", nullable: false),
                    AllowNsfwInFeed = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPrivacySettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Handle",
                table: "Users",
                columns: new[] { "TenantId", "Handle" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE AND \"Handle\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_TenantId",
                table: "UserPrivacySettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_TenantId_Id",
                table: "UserPrivacySettings",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPrivacySettings_TenantId_UserProfileId",
                table: "UserPrivacySettings",
                columns: new[] { "TenantId", "UserProfileId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPrivacySettings");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Handle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarFileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CoverFileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Handle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LinksJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PronounsJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerifiedBadgeKind",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Users");
        }
    }
}
