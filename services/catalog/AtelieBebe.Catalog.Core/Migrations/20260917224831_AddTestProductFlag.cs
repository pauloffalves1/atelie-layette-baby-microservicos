using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Catalog.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTestProductFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTest",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTest",
                table: "Products");
        }
    }
}
