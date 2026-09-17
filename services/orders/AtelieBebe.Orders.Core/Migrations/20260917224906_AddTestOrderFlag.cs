using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AtelieBebe.Orders.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTestOrderFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTest",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTest",
                table: "Orders");
        }
    }
}
