using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Catalog.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteImageSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteImages_Key",
                table: "SiteImages");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "SiteImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SiteImages_Key",
                table: "SiteImages",
                column: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SiteImages_Key",
                table: "SiteImages");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "SiteImages");

            migrationBuilder.CreateIndex(
                name: "IX_SiteImages_Key",
                table: "SiteImages",
                column: "Key",
                unique: true);
        }
    }
}
