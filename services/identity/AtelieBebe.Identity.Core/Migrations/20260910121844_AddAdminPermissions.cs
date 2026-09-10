using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Identity.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time backfill only (not a model-level default — see AdminConfiguration): every
            // admin that existed before this permission model shipped gets every permission
            // (AdminPermission.All = 1023), so nobody who could already do everything gets locked
            // out by this migration. Every admin created afterwards passes an explicit set instead.
            migrationBuilder.AddColumn<long>(
                name: "Permissions",
                table: "Admins",
                type: "bigint",
                nullable: false,
                defaultValue: 1023L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Admins");
        }
    }
}
