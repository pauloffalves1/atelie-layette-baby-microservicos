using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Identity.Core.Migrations
{
    /// <inheritdoc />
    public partial class GrantTestingToGeneralAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time backfill, same shape as AddAdminPermissions: whoever already manages the
            // admins (AdminManagement, 512) also gets Testing (1024, RF40). Testing deliberately
            // stays out of AdminPermission.All — a new admin still starts without it — but the
            // ateliê's own general admin had no way to grant it to herself: the permission gates
            // the very screens the grant is made from, and granting it through the panel is the one
            // step nobody could reach. Bitwise OR, so an admin who already holds it is unaffected.
            migrationBuilder.Sql(
                "UPDATE [Admins] SET [Permissions] = [Permissions] | 1024 WHERE [Permissions] & 512 = 512 AND [Permissions] & 1024 <> 1024;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Admins] SET [Permissions] = [Permissions] & ~CAST(1024 AS bigint);");
        }
    }
}
